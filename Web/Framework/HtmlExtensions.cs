using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.XLib.Render;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;

namespace HlidacStatu.Web.Framework
{
    public static class HtmlExtensions
    {
        public static bool IsDebug(this IHtmlHelper htmlHelper)
        {
#if DEBUG
            return true;
#else
                  return false;
#endif
        }

        public static IHtmlContent RenderBreadcrumb(this IHtmlHelper htmlHelper, Schema.NET.BreadcrumbList data)
        {
            Uri baseUri = new Uri("https://www.hlidacstatu.cz");
            if (data == null)
                return htmlHelper.Raw(string.Empty);
            if (data.ItemListElement.Count == 0)
                return htmlHelper.Raw(string.Empty);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("<ol class=\"breadcrumb\">");
            var loc = 1;
            foreach (Schema.NET.IListItem item in data.ItemListElement)
            {
                item.Position = loc;

                if (item.Item.HasOne && item.Item.First().Url.HasOne)
                {
                    sb.AppendLine($"<li><a href=\"/{baseUri.MakeRelativeUri(item.Item.First().Url.First()).ToString()}\">{htmlHelper.Encode(item.Item.First().Name)}</a></li>");
                }
                else
                    sb.AppendLine($"<li>{htmlHelper.Encode(item.Item.First().Name)}</li>");
                loc++;
            }
            sb.AppendLine("</ol>");
            sb.AppendLine("<script type=\"application/ld+json\">");
            sb.AppendLine(data.ToHtmlEscapedString());
            sb.AppendLine("</script>");


            return htmlHelper.Raw(sb.ToString());
        }


        public static Task<IHtmlContent> KIndexIconAsync(this IHtmlHelper htmlHelper, string ico, int heightInPx = 15, string hPadding = "3px", string vPadding = "0", bool showNone = false, bool useTemp = false)
        {
            return htmlHelper.KIndexIconAsync(ico, $"padding:{vPadding} {hPadding};height:{heightInPx}px;width:auto", showNone, useTemp);
        }
        public static async Task<IHtmlContent> KIndexIconAsync(this IHtmlHelper htmlHelper, string ico, string style, bool showNone = false, bool useTemp = false)
        {
            if (string.IsNullOrEmpty(ico))
                return htmlHelper.Raw("");

            ico = Util.ParseTools.NormalizeIco(ico);
            Tuple<int?, KIndexData.KIndexLabelValues> lbl = await KIndex.GetLastLabelAsync(ico, useTemp);
            if (lbl != null)
            {
                if (showNone || lbl.Item2 != KIndexData.KIndexLabelValues.None)
                    return KIndexIcon(htmlHelper, lbl.Item2, style, showNone);
            }
            return htmlHelper.Raw("");
        }
        public static IHtmlContent KIndexIcon(this IHtmlHelper htmlHelper, KIndexData.KIndexLabelValues label,
            int heightInPx = 15, string hPadding = "3px", string vPadding = "0", bool showNone = false, bool useTemp = false)
        {
            return htmlHelper.KIndexIcon(label, $"padding:{vPadding} {hPadding};height:{heightInPx}px;width:auto", showNone, useTemp: useTemp);
        }

        public static IHtmlContent KIndexIcon(this IHtmlHelper htmlHelper, KIndexData.KIndexLabelValues label,
            string style, bool showNone = false, string title = "", bool useTemp = false)
        {
            return htmlHelper.Raw(KIndexData.KindexImageIcon(label, style, showNone, title));
        }

        public static Task<IHtmlContent> KIndexLabelLinkAsync(this IHtmlHelper htmlHelper, string ico,
            int heightInPx = 15, string hPadding = "3px", string vPadding = "0", bool showNone = false,
            int? rok = null, bool linkToKindex = false)
        {
            return htmlHelper.KIndexLabelLinkAsync(ico, $"padding:{vPadding} {hPadding};height:{heightInPx}px;width:auto", showNone, rok, linkToKindex);
        }

        public static async Task<IHtmlContent> KIndexLabelLinkAsync(this IHtmlHelper htmlHelper, string ico, string style, bool showNone = false, int? rok = null, bool linkToKindex = false)
        {
            if (string.IsNullOrEmpty(ico))
                return htmlHelper.Raw("");

            ico = Util.ParseTools.NormalizeIco(ico);
            var user = htmlHelper.ViewContext.HttpContext.User;
            Tuple<int?, KIndexData.KIndexLabelValues>? kidx = await KIndex.GetLastLabelAsync(ico);
            if (kidx == null)
                return htmlHelper.Raw("");

            KIndexData.KIndexLabelValues lbl = kidx.Item2;
            return htmlHelper.KIndexLabelLinkAsync(ico, lbl, style, showNone, rok, linkToKindex: linkToKindex);
        }
        public static IHtmlContent KIndexLabelLinkAsync(this IHtmlHelper htmlHelper, string ico,
            KIndexData.KIndexLabelValues label,
            string style, bool showNone = false, int? rok = null, bool linkToKindex = false)
        {
            if (string.IsNullOrEmpty(ico))
                return htmlHelper.Raw("");

            if (linkToKindex)
            {
                if (label != KIndexData.KIndexLabelValues.None || showNone)
                {
                    if (label == KIndexData.KIndexLabelValues.None)
                        return htmlHelper.KIndexIcon(label, style, showNone);
                    else
                        return htmlHelper.Raw($"<a href='{KIndexDetailUrl(htmlHelper, ico, rok)}'>"
                        + KIndexIcon(htmlHelper, label, style, showNone).ToString()
                        + "</a>");
                }
            }
            else
            {
                if (label != KIndexData.KIndexLabelValues.None || showNone)
                {
                    if (label == KIndexData.KIndexLabelValues.None)
                        return htmlHelper.KIndexIcon(label, style, showNone);
                    else
                        return htmlHelper.Raw($"<a href='/Subjekt/{ico}'>"
                        + KIndexIcon(htmlHelper, label, style, showNone).ToString()
                        + "</a>");
                }
            }

            return htmlHelper.Raw("");

        }

        public static string KIndexDetailUrl(this IHtmlHelper htmlHelper, string ico, int? rok)
        {
            return $"/kindex/detail/{ico}{(rok.HasValue ? "?rok=" + rok.Value : "")}";
        }

        public static IHtmlContent KIndexLimitedRaw(this IHtmlHelper htmlHelper, params IHtmlContent[] htmls)
        {
            var user = htmlHelper.ViewContext.HttpContext.User;
            var s = string.Join("", htmls.Select(m => m.ToString().Replace("\n", "").Trim()));
            return htmlHelper.Raw(s);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="rok"></param>
        /// <returns></returns>
        public static int MaxKIndexYearToShow(System.Security.Principal.IPrincipal user, int? rok = null)
        {
            if (rok.HasValue)
                return Math.Min(rok.Value, MaxKIndexYearToShow(user));
            else
                return MaxKIndexYearToShow(user);
        }
        public static int MaxKIndexYearToShow(System.Security.Principal.IPrincipal user)
        {
            int lastY = Devmasters.ParseText.ToInt(Devmasters.Config.GetWebConfigValue("KIndexMaxYear"))
                ?? KIndexRepo.GetAvailableCalculationYears().Max();
            if (
                IfInRoles(user, "TK-KIndex-2021")
                || IfInRoles(user, "Admin")
                )
                return KIndexRepo.GetAvailableCalculationYears().Max();
            else
                return lastY;
        }

        public static bool ShowFutureKIndex(System.Security.Principal.IPrincipal user)
        {
            return true;
            //return IfInRoles(user, "TK-KIndex-2021");
        }

        public static Restricted ShowFutureKIndex(this IHtmlHelper self, System.Security.Principal.IPrincipal user)
        {
            return new(self, ShowFutureKIndex(user));
        }
        public static bool IfInRoles(System.Security.Principal.IPrincipal user, params string[] roles)
        {
            bool show = false;
            if (roles.Count() > 0)
            {
                if (user?.Identity?.IsAuthenticated == true)
                {
                    foreach (var r in roles)
                    {
                        if (user.IsInRole(r))
                        {
                            show = true;
                            break;
                        }
                    }
                }
            }
            else
                show = true;
            return show;
        }

        public static Restricted IfInRoles(this IHtmlHelper self, System.Security.Principal.IPrincipal user, params string[] roles)
        {
            return new(self, IfInRoles(user, roles));
        }

        public static IHtmlContent Toggleable(this IHtmlHelper htmlHelper,
            string first, string firstButton,
            string second, string secondButton)
        {
            return Toggleable(htmlHelper, new HtmlString(first), firstButton, new HtmlString(second), secondButton);
        }
        public static IHtmlContent Toggleable(this IHtmlHelper htmlHelper,
            IHtmlContent first, string firstButton,
            IHtmlContent second, string secondButton)
        {
            string random = Guid.NewGuid().ToString("N");
            var sb = new System.Text.StringBuilder();

            sb.Append($"<script>");
            sb.Append($"$(function () {{");
            sb.Append($"$('.{random}_first.btn').click(function () {{");
            sb.Append($"$('.{random}_first.content').show();");
            sb.Append($"$('.{random}_second.content').hide();");
            sb.Append($"$('.{random}_first.btn').addClass(\"btn-primary\");");
            sb.Append($"$('.{random}_second.btn').removeClass(\"btn-primary\");");
            sb.Append($"}});");
            sb.Append($"$('.{random}_second.btn').click(function () {{");
            sb.Append($"$('.{random}_first.content').hide();");
            sb.Append($"$('.{random}_second.content').show();");
            sb.Append($"$('.{random}_first.btn').removeClass(\"btn-primary\");");
            sb.Append($"$('.{random}_second.btn').addClass(\"btn-primary\");");
            sb.Append($"}});");
            sb.Append($"}});");
            sb.Append($"</script>");
            sb.Append($"<div class=\"btn btn-default {random}_first btn-primary\" style=\"border-top-right-radius: 0px;border-bottom-right-radius: 0px;\">{firstButton}</div>");
            sb.Append($"<div class=\"btn btn-default {random}_second\" style=\"border-top-left-radius: 0px;border-bottom-left-radius: 0px;\">{secondButton}</div>");
            sb.Append($"<div class=\"{random}_first content\">{first}</div>");
            sb.Append($"<div class=\"{random}_second content\" style=\"display: none; \">{second}</div>");

            return htmlHelper.Raw(sb.ToString());
        }

        public static IHtmlContent TimelineGraph(this IHtmlHelper htmlHelper,
            string name,
            string rowLabel,
            string barLabel,
            IEnumerable<(string row, string bar, DateTime? from, DateTime? to)> data,
            int height)
        {

            var stringifiedRows = data
                .Select(d => $"['{d.row}','{d.bar}',new Date({FormatDate(d.from, false)}),new Date({FormatDate(d.to, true)})]");
            string timelineDataArr = $"[{string.Join(",", stringifiedRows)}]";

            string graph = @"
            <div id='" + name + @"' style='height:" + height + @"px'></div>
            <script type=""text/javascript"" src=""https://www.gstatic.com/charts/loader.js""></script>
            <script type=""text/javascript"">
              google.charts.load('current', {'packages':['timeline'], language:'cs'});
              google.charts.setOnLoadCallback(drawChart);
              function drawChart() {
                var container = document.getElementById('timeline');
                var chart = new google.visualization.Timeline(container);
                var dataTable = new google.visualization.DataTable();

                dataTable.addColumn({ type: 'string', id: '" + rowLabel + @"' });
                dataTable.addColumn({ type: 'string', id: '" + barLabel + @"' });
                dataTable.addColumn({ type: 'date', id: 'Start' });
                dataTable.addColumn({ type: 'date', id: 'End' });
                dataTable.addRows(" + timelineDataArr + @");

                var options = {
                    avoidOverlappingGridLines: false,
                    timeline: {
                        barLabelStyle: {
                            fontSize: 10,
                            fontWeight: 'bold'
                        },
                        rowLabelStyle: {
                            fontSize: 10
                        }
                    }
                };

                chart.draw(dataTable, options);
              }
            </script>";

            return htmlHelper.Raw(graph);

            string FormatDate(DateTime? datum, bool isDatumDo)
            {
                var fixedDate = datum ??
                    (isDatumDo ? DateTime.Now : new DateTime(2000, 1, 1));

                int rok = fixedDate.Year;
                int mesic = fixedDate.Month - 1; //protože javascript
                int den = fixedDate.Day;

                return $"new Date({rok},{mesic},{den})";
            }
        }



        public static IHtmlContent PieChart(this IHtmlHelper htmlHelper,
            string title,
            SeriesTextValue data,
            int height = 300,
            string xTooltip = "Rok",
            string yTitleLeft = "Hodnota (Kč)",
            string yTitleRight = "",
            string tooltipFormat = null,
            bool showZerovalues = false
            )
        {
            string random = Guid.NewGuid().ToString("N");
            var sb = new System.Text.StringBuilder();
            if (showZerovalues == false)
            {
                data.Data = data.Data.Where(m => m.Y > 0).ToArray();
            }

            sb.AppendLine($"<div id='{random}' ></div>");
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine($"var g_{random};");
            sb.AppendLine("$(document).ready(function () {");
            //sb.AppendLine(GraphTheme());
            sb.AppendLine($"g_{random} = new Highcharts.Chart(");

            var anon = new
            {
                chart = new
                {
                    spacingTop = 30,
                    renderTo = random,
                    height = (height),
                    type = "pie",
                },
                legend = new
                {
                    enabled = false,
                    //reversed = true,
                    symbolHeight = 15,
                    symbolWidth = 15,
                    squareSymbol = true
                },
                title = new
                {
                    y = -10,
                    useHtml = true,
                    align = "left",
                    text = $"<span class=\"chart_title\">{title}</span>",
                },
                navigation = new { buttonOptions = new { enabled = false } },
                series = new SeriesTextValue[] { data },
                tooltip = new
                {
                    pointFormat = tooltipFormat ?? "{series.name}: <b>{point.y}</b>"
                }

            };


            var ser = Newtonsoft.Json.JsonConvert.SerializeObject(anon, new Newtonsoft.Json.JsonSerializerSettings() { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
            sb.Append(ser);
            sb.Append(");});");
            sb.AppendLine("</script>");
            return htmlHelper.Raw(sb.ToString());
        }

        public static IHtmlContent ColumnGraph(this IHtmlHelper htmlHelper,
            string title,
            Series[] series,
            int height = 300,
            string xTooltip = "Rok",
            string yTitleLeft = "Hodnota (Kč)",
            string yTitleRight = "",
            bool allowDecimals = false,
            bool stacked = false,
            string showStackedSummaryFormat = "{total}"
            )
        {
            string random = Guid.NewGuid().ToString("N");
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"<div id='{random}' ></div>");
            sb.AppendLine("<script type='text/javascript'>");
            sb.AppendLine($"var g_{random};");
            sb.AppendLine("$(document).ready(function () {");
            //sb.AppendLine(GraphTheme());
            sb.AppendLine($"g_{random} = new Highcharts.Chart(");

            var anon = new
            {
                chart = new
                {
                    spacingTop = 30,
                    renderTo = random,
                    height = height,
                    type = "column",
                },
                legend = new
                {
                    //enabled = false,
                    //reversed = true,
                    symbolHeight = 15,
                    symbolWidth = 15,
                    squareSymbol = true
                },
                title = new
                {
                    y = -10,
                    useHtml = true,
                    align = "left",
                    text = $"<span class=\"chart_title\">{title}</span>",
                },
                tooltip = new
                {
                    useHTML = true,
                    shared = true,
                    valueDecimals = 0,
                    headerFormat = $"<table class=\"chart_tooltip-table\"><tr><td>{xTooltip}:</td><td>{{point.key}}</td>",
                    pointFormat = "<tr><td><span class=\"chart_small-circle\" style=\" background-color: {series.color};\" ></span> {series.name}: </td><td style=\"text-align: right\"><b>{tooltip.valuePrefix}{point.y}{tooltip.valueSuffix}</b></td></tr>",
                    footerFormat = "</table>",

                },
                plotOptions = new
                {
                    series = new
                    {
                        stacking = stacked ? "normal" : (string)null
                    }
                },
                xAxis = new
                {
                    labels = new
                    {
                        staggerLines = 1
                    },
                    title = new
                    {
                        text = ""
                    }
                },
                yAxis = new object[]
                {
                    new
                    {
                        stackLabels = new
                        {
                            enabled = (stacked && showStackedSummaryFormat != null),
                            format = showStackedSummaryFormat
                        },
                        allowDecimals = allowDecimals,
                        min = 0,
                        lineWidth = 0,
                        tickWidth = 1,
                        title = new
                        {
                            align = "high",
                            offset = 0,
                            rotation = 0,
                            y = -20,
                            text = yTitleLeft,
                        },
                        type = "linear",
                    },
                    new
                    {
                        opposite = true,
                        allowDecimals = allowDecimals,
                        min = 0,
                        lineWidth = 0,
                        tickWidth = 1,
                        title = new
                        {
                            align = "high",
                            offset = 0,
                            rotation = 0,
                            y = -20,
                            text = yTitleRight,
                        },
                        type = "linear",
                        gridLineWidth = 0
                    },
                },
                navigation = new { buttonOptions = new { enabled = false } },
                series = series

            };


            var ser = Newtonsoft.Json.JsonConvert.SerializeObject(anon);
            sb.Append(ser);
            sb.Append(");});");
            sb.AppendLine("</script>");
            return htmlHelper.Raw(sb.ToString());
        }

        public static IHtmlContent GraphTheme(this IHtmlHelper htmlHelper)
        {
            var sb = new System.Text.StringBuilder();

            var fontStyle = new
            {
                fontFamily = "Cabin",
                fontSize = 14,
                color = "#AEBCCB"
            };

            var options = new
            {
                chart = new
                {
                    style = fontStyle
                },
                colors = new[]
                {
                    "#DDE3E9", "#AFB9C5", "#2975DC", "#E76605"
                },
                tooltip = new
                {
                    backgroundColor = "#FFFFFF",
                    borderWidth = 1,
                    shadow = false,
                },
                plotOptions = new
                {
                    line = new
                    {
                        animation = true,
                        borderWidth = 0,
                        groupPadding = 0,
                        shadow = false
                    },
                    column = new
                    {
                        animation = true,
                        borderWidth = 0,
                        grouping = false,
                        groupPadding = 0,
                        shadow = false
                    },
                    bar = new
                    {
                        animation = true,
                        borderWidth = 0,
                        grouping = false,
                        groupPadding = 0,
                        shadow = false
                    },
                    series = new
                    {
                        states = new
                        {
                            hover = new
                            {
                                brightness = -0.3 // darken
                            }
                        }
                    }
                },
                xAxis = new
                {
                    labels = new
                    {
                        style = fontStyle
                    },
                    title = new
                    {
                        style = fontStyle
                    }
                },
                yAxis = new
                {
                    labels = new
                    {
                        style = fontStyle
                    },
                    title = new
                    {
                        style = fontStyle
                    }
                },
                legend = new
                {
                    itemStyle = fontStyle
                },
                //title - nastaven přes css

            };

            string optionsSerialized = Newtonsoft.Json.JsonConvert.SerializeObject(options);

            sb.AppendLine("<script>");
            sb.AppendLine($"Highcharts.setOptions({optionsSerialized});");
            sb.AppendLine("</script>");

            //return $"Highcharts.setOptions({optionsSerialized});";
            return htmlHelper.Raw(sb.ToString());
        }

        public static string RenderRawHtml(this IHtmlContent content)
        {
            using (var writer = new System.IO.StringWriter())
            {
                content.WriteTo(writer, System.Text.Encodings.Web.HtmlEncoder.Default);
                return writer.ToString();
            }
        }

        public static object DatatableOptionsObject(
            bool paging = true, int? pageLength = 10, bool info = false, bool filter = true, string dom = "Bfrtip",
            bool lengthChange = false, bool exportButtons = true, bool searching=true,
            bool ordering = true, int? orderColumnIdx = null, string? orderDirection = null,
            Dictionary<string,object> customObjs = null
            )
        {
            dynamic conf = new
            {
                language = new
                {
                    url = "//cdn.datatables.net/plug-ins/1.13.4/i18n/cs.json"
                },
                lengthChange = lengthChange,
                paging = paging,
                pageLength = pageLength.HasValue? pageLength.Value : (int?)null,
                info = info,
                filter = filter,
                searching = searching,
                dom = dom,
                buttons = exportButtons==false ? null : new[] {
                    new {
                        extend ="csvHtml5",
                        text = "Export do CSV",
                        exportOptions = new
                            {
                                modifier = new {search="none" }
                            }
                    },
                    new {
                        extend ="excelHtml5",
                        text = "Export do Excelu",
                        exportOptions = new
                            {
                                modifier = new {search="none" }
                            }
                    },
                },
                ordering = ordering,
                order = orderColumnIdx.HasValue == false ? null : new[] {new object[] {orderColumnIdx.Value, orderDirection}}
            };

            if (customObjs?.Count > 0)
            {
                var expando = conf as IDictionary<string, object>;
                foreach (var kv in customObjs)
                {
                    if (expando.ContainsKey(kv.Key))
                        expando[kv.Key] = kv.Value;
                    else
                        expando.TryAdd(kv.Key, kv.Value);
                }
                return expando;
            }
            else
                return conf;

        }
        public static string DatatableOptions(
            bool paging = true, int? pageLength = 10, bool info = false, bool filter = true, string dom = "Bfrtip",
            bool lengthChange = false, bool exportButtons = true, bool searching = true,
            bool ordering = true, int? orderColumnIdx = null, string? orderDirection = null)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(
                DatatableOptionsObject(paging, pageLength,info,filter,dom,lengthChange,exportButtons,searching, ordering, orderColumnIdx,orderDirection)
                );
        }

        public static IHtmlContent DataToHTMLTable<T>(this IHtmlHelper htmlHelper,
            ReportDataSource<T> rds,
            string tableId = "",
            string dataTableOptions = @"{
                         'language': {
                            'url': '//cdn.datatables.net/plug-ins/1.13.4/i18n/cs.json'
                        },
                        'order': [],
                        'lengthChange': false,
                        'info': false,
                        }",
            string customTableHeader = null)
        {
            string _tableId = tableId;
            if (string.IsNullOrEmpty(tableId))
            {
                _tableId = Devmasters.TextUtil.GenRandomString("abcdefghijklmnopqrstuvwxyz", 10);
            }

            if (dataTableOptions.Contains("#id#"))
            {
                dataTableOptions = dataTableOptions.Replace("#id#", _tableId);
            }

            var sb = new System.Text.StringBuilder(1024);

            sb.AppendLine(@"<script>
var tbl_" + _tableId + @";
$(document).ready(function () {
    tbl_" + _tableId + @" = $('#" + _tableId + @"').DataTable(" + dataTableOptions + @");

});
</script>");

            if (!string.IsNullOrEmpty(rds?.Title))
            {
                sb.AppendFormat("<h3>{0}</h3>", rds?.Title ?? "");
            }
            sb.AppendFormat("<table id=\"{0}\" class=\"table-sorted table table-bordered table-striped\">", _tableId);
            if (customTableHeader == null)
            {
                sb.Append("<thead><tr>");
                foreach (var column in rds.Columns)
                {
                    sb.AppendFormat("<th>{0}</th>", column.Name);
                }
                sb.Append("</tr></thead>");
            }
            else
            {
                sb.AppendFormat(customTableHeader, _tableId);
            }
            sb.Append("<tbody class=\"list\">");
            foreach (var row in rds.Data)
            {
                sb.Append("<tr>");
                foreach (var d in row)
                {
                    sb.AppendFormat("<td {2} class=\"{0}\">{1}</td>",
                        d.Column.CssClass,
                        d.Column.HtmlRender(d.Value),
                        string.IsNullOrEmpty(d.Column.OrderValueRender(d.Value))
                                ? string.Empty : string.Format("data-order=\"{0}\"", d.Column.OrderValueRender(d.Value))
                        );

                }
                sb.Append("</tr>");
            }
            sb.Append("</table>");
            return htmlHelper.Raw(sb.ToString());
        }


        public static IHtmlContent SubjektTypTrojice(this IHtmlHelper self, Firma firma, string htmlProOVM, string htmlProStatniFirmu, string htmlProSoukromou)
        {
            if (firma == null)
                return self.Raw("");
            if (firma.JsemOVM())
                return self.Raw(htmlProOVM);
            else if (firma.JsemStatniFirma())
                return self.Raw(htmlProStatniFirmu);
            else
                return self.Raw(htmlProSoukromou);
        }

        public static IHtmlContent SocialLinkWithIcon(this IHtmlHelper self,
            OsobaEvent.SocialNetwork? socialnet, string value, string ico,
            string htmlclass = "fa-2x", bool intoNewTab = true)
        {
            string hsiconstyle = "height:1em";
            if (htmlclass == "fa-2x")
                hsiconstyle = "height:1.6em";

            switch (socialnet)
            {
                case OsobaEvent.SocialNetwork.Twitter:
                    return self.Raw($"<a title='Twitter' {(intoNewTab ? "target='_blank'" : "")} href='https://www.twitter.com/{value}'><i class='fab fa-twitter-square {htmlclass}'></i></a>");
                case OsobaEvent.SocialNetwork.Facebook_page:
                case OsobaEvent.SocialNetwork.Facebook_profile:
                    return self.Raw($"<a title='Facebook' {(intoNewTab ? "target='_blank'" : "")}href='https://www.facebook.com/{value}'><i class='fab fa-facebook-square {htmlclass}'></i></a>");
                case OsobaEvent.SocialNetwork.Instagram:
                    return self.Raw($"<a title='Instagram' {(intoNewTab ? "target='_blank'" : "")}href='https://www.instagram.com/{value}'><i class='fab fa-instagram-square {htmlclass}'></i></a>");
                case OsobaEvent.SocialNetwork.WWW:
                    return self.Raw($"<a title='Webová stránka' {(intoNewTab ? "target='_blank'" : "")}href='{value}'><i class='fas fa-link {htmlclass}'></i></a>");
                case OsobaEvent.SocialNetwork.Youtube:
                    return self.Raw($"<a title='Youtube' {(intoNewTab ? "target='_blank'" : "")}href='{value}'><i class='fab fa-youtube {htmlclass}'></i></a>");
                case OsobaEvent.SocialNetwork.Zaznam_zastupitelstva:
                    return self.Raw(
                        $"<a title='Záznam zastupitelstva' {(intoNewTab ? "target='_blank'" : "")}href='{value}'><i class='fab fa-youtube {htmlclass}'></i></a>"
                        + $"<a title='Přepis záznamu zastupitelstva na Hlídači státu' {(intoNewTab ? "target='_blank'" : "")}href='/data/Hledat/zasedani-zastupitelstev?Q=ico:{ico}&order=datum%20desc'><img src='/content/img/Hlidac-statu-ctverec-notext.svg' style='{hsiconstyle};width:auto;vertical-align:baseline;padding-left:2px;padding-right:2px;' /></a>");
                default:
                    return self.Raw("");
            }
        }

        public static IHtmlContent RenderVazby(this IHtmlHelper self, HlidacStatu.DS.Graphs.Graph.Edge[] vazbyToRender)
        {
            if (vazbyToRender == null)
            {
                return self.Raw("");
            }
            if (vazbyToRender.Count() == 0)
            {
                return self.Raw("");
            }

            if (vazbyToRender.Count() == 1)
                return self.Raw($"{vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}");
            else
            {
                return self.Raw("Nepřímá vazba přes:<br/><small>"
                    + $"{vazbyToRender.First().From?.PrintName()} {vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}"
                    + $" → "
                    + vazbyToRender.Skip(1).Select(m => m.Descr + " v " + m.To.PrintName()).Aggregate((f, s) => f + " → " + s)
                    + "</small>"
                    );
            }
        }

    }
}