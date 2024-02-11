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

namespace HlidacStatu.Lib.Web.UI
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


    }
}