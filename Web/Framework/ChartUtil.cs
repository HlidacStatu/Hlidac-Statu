using HlidacStatu.XLib.Render;

using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.Web.Framework
{
    public static class ChartUtil
    {
        public static IHtmlContent SimpleStackedChart(
            IEnumerable<(string name, IEnumerable<ReportDataTimeValue> values)> data,
            string title, string valueName,
            int height, string addStyle = "", int? minY = null, bool showMarker = true, string chartType = "column")
        {
            string containerId = "chart_" + Guid.NewGuid().ToString("N");
            List<string> sdata = new List<string>();
            foreach (var d in data)
            {
                sdata.Add(
                    $"{{ name : '{d.name}', data :["
                    + d.values
                        .Select(m =>
                            $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]")
                        .Aggregate((f, s) => f + "," + s)
                    + $"]}}"
                );
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<div id='{containerId}' style='height:{height}px;{addStyle}'></div>");
            sb.AppendLine(@"<script>
Highcharts.chart('@containerId', {
    chart: {
        plotBackgroundColor: null,
        plotBorderWidth: 0,
        plotShadow: false,
        type: '" + chartType + @"'
    },
 title: {
        text: '" + title + @"'
    },

    yAxis: {
        title: {
            text: 'Počet'
        }");
            if (minY.HasValue)
            {
                sb.AppendLine($", min: {minY.Value}");
            }

            sb.AppendLine(@"},
        tooltip: {
        xDateFormat: '%d. %m. %Y %H:%M',
        shared: true
    },
    plotOptions: {
        series: {
            stacking: 'normal'");
            if (showMarker)
            {
                sb.AppendLine("      ,marker: { radius: 3, enabled: true }");
            }

            sb.AppendLine(@"        }
        },
xAxis: {
    type: 'datetime',
        tickInterval: 24 * 3600 * 1000
    },

    legend: {
    enabled:false
    },
    series: [
        " + string.Join(", ", sdata) + @"
    ]
});</script>");

            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent SimpleLineChart(
            IEnumerable<(string name, IEnumerable<ReportDataTimeValue> values)> data,
            string title, string valueName,
            int height, string addStyle = "", int? minY = null, bool showMarker = true)
        {
            string containerId = "chart_" + Guid.NewGuid().ToString("N");
            List<string> sdata = new List<string>();
            foreach (var d in data)
            {
                sdata.Add(
                    $"{{ name : '{d.name}', data :["
                    + d.values
                        .Select(m =>
                            $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]")
                        .Aggregate((f, s) => f + "," + s)
                    + $"]}}"
                );
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"<div id='" + containerId + @"' style='height:" + height + @"px;" + addStyle + @"'></div>
    <script>
Highcharts.chart('" + containerId + @"', {
    chart: {
        plotBackgroundColor: null,
        plotBorderWidth: 0,
        plotShadow: false
    },
 title: {
        text: '" + title + @"'
    },

    yAxis: {
        title: {
            text: 'Počet'
        }");

            if (minY.HasValue)
            {
                sb.AppendLine($", min: {minY.Value}");
            }

            sb.AppendLine(@"},
        tooltip: {
        xDateFormat: '%d. %m. %Y %H:%M',
        shared: true
    },");
            if (showMarker)
            {
                sb.AppendLine(@"
    plotOptions: {
        series: {
            marker: {
                enabled: true
                    }
            }
        },
    ");
            }

            sb.AppendLine(@"xAxis: {
    type: 'datetime',
        tickInterval: 24 * 3600 * 1000
    },

    legend: {
    enabled:false
    },
    series: [
        " + string.Join(",", sdata) + @"
    ]
});</script>");
            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent SemiCircleDonut(IEnumerable<Tuple<string, decimal>> data,
            string title, string valueName,
            int height, string addStyle = "")
        {
            //https://jsfiddle.net/gh/get/library/pure/highcharts/highcharts/tree/master/samples/highcharts/demo/pie-semi-circle
            string containerId = "chart_" + Guid.NewGuid().ToString("N");
            string sdata = data
                .Select(m => $"['{m.Item1}',{m.Item2.ToString(HlidacStatu.Util.Consts.enCulture)}]")
                .Aggregate((f, s) => f + "," + s);

            var sb = @"
    <div id='" + containerId + @"' style='height:" + height + @"px;" + addStyle + @"'></div>
    <script>
Highcharts.chart('" + containerId + @"', {
    chart: {
        plotBackgroundColor: null,
        plotBorderWidth: 0,
        plotShadow: false
    },
    title: {
    text: '" + title + @"',
        align: 'center',
        verticalAlign: 'middle',
        y: 60
    },
tooltip: {
pointFormat: '{series.name}: <b>{point.percentage:.1f}%</b>'
    },
    accessibility: {
point: {
    valueSuffix: '%'
        }
},
    plotOptions: {
pie: {
    dataLabels: {
        enabled: false,
                distance: -50,
                style: {
            fontWeight: 'bold',
                    color: 'white'
                }
        },
            startAngle: -90,
            endAngle: 90,
            center: ['50%', '75%'],
            size: '150%'
        }
        },
    series: [{
        type: 'pie',
        name: '" + valueName + @"',
        innerSize: '40%',
        data: [" + sdata + @"]
    }]
});</script>
";
            return new HtmlString(sb);
        }

        public static IHtmlContent RenderSimpleTimeChart(IEnumerable<ReportDataTimeValue> data,
            int width, int height, string valueName,
            bool showMaxLine = false)
        {
            if (data == null)
            {
                return HtmlString.Empty;
            }

            if (data.Count() == 0)
            {
                return HtmlString.Empty;
            }

            string containerId = "chart_" + Guid.NewGuid().ToString("N");
            string sMaxVal = data.Max(m => m.Value).ToString(HlidacStatu.Util.Consts.enCulture);
            string sdata = data
                .Select(m =>
                    $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]")
                .Aggregate((f, s) => f + "," + s);

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<div id='{containerId}' style='height:{height}px;width:{width}px'></div>");
            sb.AppendLine("<script>");

            sb.AppendLine($"Highcharts.chart('{containerId}', {{");
            sb.AppendLine("chart: { zoomType: 'x' },");
            sb.AppendLine("title: null,");
            sb.AppendLine("subtitle: null,");
            sb.AppendLine("xAxis: { type: 'datetime', labels : { enabled : false}, visible: false },");
            sb.AppendLine(
                "yAxis: { tickInterval : 0, labels : { enabled : false}, title : { enabled: false }, tickInterval: 0, gridLineWidth: 0,");
            if (showMaxLine)
            {
                sb.AppendLine("plotLines: [{");
                sb.AppendLine("        label: {text:'" + sMaxVal + "', y: 15},");
                sb.AppendLine("            color: '#3977d5', value: " + sMaxVal + ", width:1, dashStyle: 'Dot'");
                sb.AppendLine("        }],");
                sb.AppendLine("        },");
            }

            sb.AppendLine("legend: { enabled: false },");
            sb.AppendLine("plotOptions: {");
            sb.AppendLine("area: { color: '#3977d5',");
            sb.AppendLine("        fillColor: {");
            sb.AppendLine("          linearGradient: { x1: 0, y1: 0, x2: 0, y2: 1 },");
            sb.AppendLine("            stops: [");
            sb.AppendLine("                [0, Highcharts.getOptions().colors[0]],");
            sb.AppendLine(
                "                [1, Highcharts.Color(Highcharts.getOptions().colors[0]).setOpacity(0).get('rgba')]");
            sb.AppendLine("            ]");
            sb.AppendLine("        },");
            sb.AppendLine("        marker:  { radius: 2 }, lineColor: '#3977d5', lineWidth: 5,");
            sb.AppendLine("        states: { hover: { lineWidth: 5 } }, threshold: null");
            sb.AppendLine("    }");
            sb.AppendLine("},");

            sb.AppendLine("series: [{");
            sb.AppendLine("type: 'area',");
            sb.AppendLine($"name: '{valueName}',");
            sb.AppendLine("data: [{sdata}]");
            sb.AppendLine("}]");
            sb.AppendLine("});");
            sb.AppendLine("</script>");

            return new HtmlString(sb.ToString());
        }

        public static IHtmlContent SimpleBarChart<T>(this IHtmlHelper htmlHelper, bool columnType, bool timeData,
                int height,
                string containerId, string xAxisName, string yAxisName,
                ReportDataSource<T> rds, string tooltipValueSuffix = "",
                string xValueFormat = null, string yValueFormat = null, string tooltipFormatFull = null,
                string loadEvents = null, string backgroundColor = null, string addStyle = null, bool stacking = false
            )
        //where T : class
        {
            return SimpleBarChart(htmlHelper, columnType, timeData, height,
                containerId, xAxisName, yAxisName,
                new ReportDataSource<T>[] { rds },
                tooltipValueSuffix, xValueFormat, yValueFormat, tooltipFormatFull,
                backgroundColor, addStyle, stacking: stacking);
        }

        public static IHtmlContent SimpleBarChart<T>(this IHtmlHelper htmlHelper,
            bool columnType, bool timeData, int height,
            string containerId, string xAxisName, string yAxisName,
            ReportDataSource<T>[] rds, string tooltipValueSuffix = "",
            string xValueFormat = null, string yValueFormat = null, string tooltipFormatFull = null,
            string loadEvents = null, string backgroundColor = null, string addStyle = null, bool stacking = false
        )
        {
            StringBuilder sb = new();

            string stackingElement = stacking ? ", \"series\": {\"stacking\": \"normal\"}" : "";

            addStyle = addStyle ?? "overflow:hidden;";

            if (string.IsNullOrEmpty(containerId))
                containerId = "chart" + Guid.NewGuid().ToString("N");
            rds = rds.Where(d => d != null).ToArray();

            if (string.IsNullOrEmpty(xValueFormat))
                xValueFormat = timeData ? "{value:%b \'%y}" : "{value}";
            if (string.IsNullOrEmpty(yValueFormat))
                yValueFormat = "{point.y:,.0f}";
            string tooltipFormat = xValueFormat.Replace("{value", "{point.x")
                                   + ": " + yValueFormat
                                   + (!string.IsNullOrEmpty(tooltipValueSuffix) ? " " + tooltipValueSuffix : "");

            if (tooltipFormatFull != null)
            {
                tooltipFormat = tooltipFormatFull;
            }

            sb.AppendLine($"");
            sb.AppendLine($"<div id=\"{containerId}\" style=\"height:{height}px;{addStyle}\"></div>");
            sb.AppendLine($"<script>");
            sb.AppendLine($"var price_year_chart;");
            sb.AppendLine("$(document).ready(function() {");
            sb.AppendLine($"price_year_chart = new Highcharts.chart('{containerId}',");
            sb.AppendLine("{ chart: {");
            sb.AppendLine($"    renderTo:\"{containerId}\",");
            sb.AppendLine($"    height:\"{(rds.Count() > 1 ? height + 62 : height)}\",");
            sb.AppendLine($"    type:\"{(columnType ? "column" : "bar")}\"");
            sb.AppendLine("},");
            sb.AppendLine("\"legend\": { \"enabled\": " + ((rds.Count() > 1) ? "true" : "false") +
                          ",\"reversed\": true},");
            sb.AppendLine(
                $"\"plotOptions\": {{ \"bar\": {{ \"animation\": true, \"borderWidth\": 0, \"groupPadding\": 0, \"shadow\": true }}, \"column\": {{ \"animation\": true, \"borderWidth\": 0, \"groupPadding\": 0, \"shadow\": true }}{stackingElement} }}, ");
            sb.AppendLine("title: {text : undefined},");
            sb.AppendLine("\"tooltip\": { \"headerFormat\": \"\", \"pointFormat\": \"" + tooltipFormat +
                          "\", \"useHTML\": true }, ");
            sb.AppendLine("\"xAxis\": {");
            if (timeData == false)
            {
                sb.AppendLine("	\"categories\": [" +
                 string.Join(",",
                         rds[0].Data.Select(v => "\"" + v[0].Column.TextRender(v[0].Value) + "\"")
                         ) + "],"
                  );
            }
            sb.AppendLine("			\"labels\": {");
            sb.AppendLine($"		\"format\": \"{xValueFormat}\",");
            sb.AppendLine("				\"staggerLines\": 1");
            sb.AppendLine("			},");
            sb.AppendLine("			\"title\": {");
            sb.AppendLine($"		\"text\": \"{xAxisName}\"");
            sb.AppendLine("			},");
            sb.AppendLine($"		\"type\": \"{(timeData ? "datetime" : "category")}\"");
            sb.AppendLine("		},");
            sb.AppendLine("\"yAxis\": {");
            sb.AppendLine("	\"min\": 0,");
            sb.AppendLine("			\"title\": {");
            sb.AppendLine($"		\"text\": \"{yAxisName}\"");
            sb.AppendLine("			},");
            sb.AppendLine("			\"type\": \"linear\"");
            sb.AppendLine("		},");
            sb.AppendLine("\"navigation\": { \"buttonOptions\": { \"enabled\": false } }, ");
            sb.AppendLine("					\"series\": [{");

            foreach (var rdsItem in rds)
            {
                sb.AppendLine($"				\"name\":\"{rdsItem.Title}\",");
                sb.AppendLine($"				\"data\":[");
                foreach (var item in rdsItem.Data)
                {
                    sb.AppendLine(
                        $"[{item[0].Column.ValueRender(item[0].Value)},{item[1].Column.ValueRender(item[1].Value)}],");
                }

                sb.AppendLine("                          ],");
            }

            sb.AppendLine("					}]");
            sb.AppendLine(" 	  });");
            sb.AppendLine("	});");
            sb.AppendLine("</script>");


            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent SimpleColumnChart(this IHtmlHelper htmlHelper, IEnumerable<(string name,
                IEnumerable<ReportDataTimeValue> values)> data,
            string title, string valueName,
            int height, string addStyle = "", string chartType = "column",
            int? tickInterval = null //tickInterval: 24 * 3600 * 1000
        )
        {
            string containerId = "chart_" + Guid.NewGuid().ToString("N");
            List<string> sdata = new();
            StringBuilder sb = new();
            foreach (var d in data)
            {
                sdata.Add(
                    $"{{ name : '{d.name}', data :["
                    + d.values
                        .Select(m =>
                            $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]")
                        .Aggregate((f, s) => f + "," + s)
                    + $"]}}"
                );
            }

            sb.AppendLine($"");
            sb.AppendLine($"<div id=\"{containerId}\" style=\"height:{height}px;{addStyle}\"></div>");
            sb.AppendLine($"<script>");
            sb.AppendLine($"Highcharts.chart('{containerId}', {{");
            sb.AppendLine("chart: {");
            sb.AppendLine($"plotBackgroundColor: null,");
            sb.AppendLine($"plotBorderWidth: 0,");
            sb.AppendLine($"plotShadow: false,");
            sb.AppendLine($"type: '{chartType}'");
            sb.AppendLine("},");
            sb.AppendLine("title: {");
            sb.AppendLine($"text: '{title}'");
            sb.AppendLine("},");

            sb.AppendLine("yAxis: {");
            sb.AppendLine("    title: {");
            sb.AppendLine("       text: 'Počet'");
            sb.AppendLine("    }");
            sb.AppendLine("},");
            sb.AppendLine("tooltip: {");
            sb.AppendLine($"xDateFormat: '%d. %m. %Y %H:%M',");
            sb.AppendLine($"shared: true");
            sb.AppendLine("},");

            sb.AppendLine("xAxis: {");
            sb.AppendLine($"type: 'datetime'");
            if (tickInterval.HasValue)
            {
                sb.AppendLine($"tickInterval:{tickInterval.Value.ToString("D")}");
            }

            sb.AppendLine($"}},");

            sb.AppendLine(@"legend: { enabled:false },");
            sb.AppendLine($"series: [");
            sb.AppendLine(string.Join(",", sdata));
            sb.AppendLine($"]");
            sb.AppendLine("});</script>");

            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent RenderReportTableT<T>(string title, ReportDataSource<T> item,
                string JsDataTableOptions = null, string JsDataTableId = null)
        //  where T : class
        {
            return DataToHTMLTable(title, item, JsDataTableId, JsDataTableOptions);
        }


        public static IHtmlContent DataToHTMLTable<T>(string title, ReportDataSource<T> rds,
                string tableId = "", string dataTableOptions = "", string customTableHeader = null)
        //   where T : class
        {
            System.Text.StringBuilder sb = new(1024);
            string _tableId = tableId;
            if (string.IsNullOrEmpty(tableId))
            {
                _tableId = Devmasters.TextUtil.GenRandomString("abcdefghijklmnopqrstuvwxyz", 10);
            }

            sb.AppendLine(@"<script>
var tbl_" + _tableId + @";
$(document).ready(function () {
tbl_" + _tableId + @" = $('#" + _tableId + @"').DataTable(" + dataTableOptions + @");
});
</script>");

            sb.AppendFormat("<h3>{0}</h3>", rds?.Title ?? "");
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
                            ? string.Empty
                            : string.Format("data-order=\"{0}\"", d.Column.OrderValueRender(d.Value))
                    );
                }

                sb.Append("</tr>");
            }

            sb.Append("</table>");
            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent RenderReport(ReportModel.QueueItem item,
            string JsDataTableOptions, string JsDataTableId = null)
        {
            if (item.Type == ReportModel.QueueItem.types.chart)
            {
                return RenderReportChart(item.Title, item.Data as IHtmlContent);
            }
            else
            {
                return RenderReportTableObj(item.Title, item, JsDataTableOptions, JsDataTableId);
            }
        }

        public static IHtmlContent RenderReportTableObj(string title, ReportModel.QueueItem item,
            string JsDataTableOptions, string JsDataTableId = null)
        {
            return RenderReportTable<object>(title, item, JsDataTableOptions, JsDataTableId);
        }

        public static IHtmlContent RenderReportTable<T>(string title, ReportModel.QueueItem item,
                string JsDataTableOptions, string JsDataTableId = null)
        //where T : class
        {
            if (item.Type == ReportModel.QueueItem.types.table)
            {
                return RenderReportTableT(title, item.Data as ReportDataSource<T>,
                    JsDataTableOptions, JsDataTableId);
            }

            if (item.Type == ReportModel.QueueItem.types.chart)
            {
                throw new ArgumentOutOfRangeException();
            }

            return new HtmlString("");
        }

        public static IHtmlContent RenderReportChart(string title, IHtmlContent chart)
        {
            return new HtmlString(@"<div class='col-md-12'>"
                                  + "<div class='panel panel-default'>"
                                  + "<div class='panel-heading'>" + title + "</div><div class='panel-body'>"
                                  + chart.ToString()
                                  + "</div>"
                                  + "</div>"
                                  + "</div>");
        }


        public static IHtmlContent RenderReport(string title, ReportModel.QueueItem.types type,
            object data, string JsDataTableOptions, string JsDataTableId = null)
        {
            if (type == ReportModel.QueueItem.types.chart)
            {
                return RenderReportChart(title, data as IHtmlContent);
            }
            else
            {
                return RenderReport(
                    new ReportModel.QueueItem() { Title = title, Type = type, Data = data },
                    JsDataTableOptions, JsDataTableId);
            }
        }


    }
}