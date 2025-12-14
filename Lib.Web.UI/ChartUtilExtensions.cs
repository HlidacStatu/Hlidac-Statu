using HlidacStatu.XLib.Render;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.Lib.Web.UI
{
    public static class ChartUtilExtensions
    {
        public static IHtmlContent TimelineGraph(this IHtmlHelper htmlHelper,
            string name,
            string rowLabel,
            string barLabel,
            IEnumerable<(string row, string bar, DateTime? from, DateTime? to)> data,
            int height)
        {
            var stringifiedRows = data
                .Select(d =>
                    $"['{d.row}','{d.bar}',new Date({FormatDate(d.from, false)}),new Date({FormatDate(d.to, true)})]");
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


            var ser = Newtonsoft.Json.JsonConvert.SerializeObject(anon,
                new Newtonsoft.Json.JsonSerializerSettings()
                    { NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore });
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
            string showStackedSummaryFormat = "{total}",
            bool showLegend = true
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
                    enabled = showLegend,
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
                    headerFormat =
                        $"<table class=\"chart_tooltip-table\"><tr><td>{xTooltip}:</td><td>{{point.key}}</td>",
                    pointFormat =
                        "<tr><td><span class=\"chart_small-circle\" style=\" background-color: {series.color};\" ></span> {series.name}: </td><td style=\"text-align: right\"><b>{tooltip.valuePrefix}{point.y}{tooltip.valueSuffix}</b></td></tr>",
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
                    + string.Join(",",d.values
                                          .Select(m =>
                                              $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]"))
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
                    + string.Join(",", d.values
                                          .Select(m =>
                                              $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]"))
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
            string sdata = string.Join(",", data
                .Select(m => $"['{m.Item1.Replace("'", "\'")}',{m.Item2.ToString(HlidacStatu.Util.Consts.enCulture)}]"));

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
            string sdata = string.Join(",", data
                .Select(m =>
                    $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]"));

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

        public static IHtmlContent SimpleBarChart<T>(this IHtmlHelper htmlHelper,
                bool columnType, bool timeData, int height,
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
            sb.AppendLine("					\"series\": [");

            foreach (var rdsItem in rds)
            {
                sb.AppendLine($"				{{\"name\":\"{rdsItem.Title}\",");
                sb.AppendLine($"				\"data\":[");
                foreach (var item in rdsItem.Data)
                {
                    sb.AppendLine(
                        $"[{item[0].Column.ValueRender(item[0].Value)},{item[1].Column.ValueRender(item[1].Value)}],");
                }

                sb.AppendLine("                          ]},");
            }

            sb.AppendLine("					]");
            sb.AppendLine(" 	  });");
            sb.AppendLine("	});");
            sb.AppendLine("</script>");


            return new HtmlString(sb.ToString());
        }


        public static IHtmlContent SimpleLineColumnChart(this IHtmlHelper htmlHelper, IEnumerable<(string name,
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
                    + string.Join(",", d.values
                        .Select(m =>
                            $"[Date.UTC({m.Date.Year},{m.Date.Month - 1},{m.Date.Day},{m.Date.Hour},{m.Date.Minute}),{m.Value.ToString(HlidacStatu.Util.Consts.enCulture)}]"))
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
            if (item == null)
            {
                return HtmlString.Empty;
            }

            item.Title = title;
            return HtmlExtensions.DataToHTMLTable(item, JsDataTableId, JsDataTableOptions);
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