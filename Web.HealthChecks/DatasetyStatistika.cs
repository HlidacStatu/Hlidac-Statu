using Devmasters.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class DatasetyStatistika : IHealthCheck
    {
        Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<DatasetStat>> statistiky =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<DatasetStat>>(
                TimeSpan.FromMinutes(60), "DatasetStat_all", async _ =>
                {
                    try
                    {
                        List<DatasetStat> res = new List<DatasetStat>();
                        foreach (var ds in HlidacStatu.Datasets.DataSetDB.ProductionDataSets.Get())
                        {
                            var item = new DatasetStat();
                            item.Dataset = ds;

                            var allrec = await ds.SearchDataAsync("*", 1, 1, sort: "DbCreated desc",
                                exactNumOfResults: true);
                            item.PocetCelkem = allrec.Total;
                            if (allrec.Total > 0)
                            {
                                item.PosledniZmena = (DateTime?)allrec.Result.First().DbCreated;
                            }


                            foreach (var interval in EnumsNET.Enums.GetMembers<IntervalEnum>())
                            {
                                DateTime dateFrom = DateTime.Now.Date;
                                DateTime dateTo = DateTime.Now.Date;
                                string dateQ = "";
                                switch (interval.Value)
                                {
                                    case IntervalEnum.Day:
                                        dateFrom = dateTo.AddDays(-1);
                                        dateQ = $"{dateFrom:yyyy-MM-dd}";
                                        break;
                                    case IntervalEnum.Week:
                                        dateFrom = dateTo.AddDays(-7);
                                        dateQ = $"[{dateFrom:yyyy-MM-dd} TO {dateTo:yyyy-MM-dd}]";
                                        break;
                                    case IntervalEnum.Month:
                                        dateFrom = dateTo.AddMonths(-1);
                                        dateQ = $"[{dateFrom:yyyy-MM-dd} TO {dateTo:yyyy-MM-dd}]";
                                        break;
                                    case IntervalEnum.Year:
                                        dateFrom = dateTo.AddYears(-1);
                                        dateQ = $"[{dateFrom:yyyy-MM-dd} TO {dateTo:yyyy-MM-dd}]";
                                        break;
                                    default:
                                        break;
                                }

                                var pocet = await ds.SearchDataAsync(
                                    $"DbCreated:[{DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")} TO *]", 1, 0,
                                    exactNumOfResults: true);
                                if (pocet.IsValid)
                                    item.PocetZaInterval.Add(interval.Value, pocet.Total);
                                else
                                    item.PocetZaInterval.Add(interval.Value, -1);
                            }

                            res.Add(item);
                        }

                        return res;
                    }
                    catch (Exception)
                    {
                        return new List<DatasetStat>();
                    }
                }
            );

        private class DatasetStat
        {
            public Datasets.DataSet Dataset { get; set; }
            public long PocetCelkem { get; set; }
            public DateTime? PosledniZmena { get; set; }
            public Dictionary<IntervalEnum, long> PocetZaInterval { get; set; } = new Dictionary<IntervalEnum, long>();
        }

        [Devmasters.Enums.ShowNiceDisplayName]
        public enum IntervalEnum
        {
            [Devmasters.Enums.NiceDisplayName("Den")]
            Day,

            [Devmasters.Enums.NiceDisplayName("Týden")]
            Week,

            [Devmasters.Enums.NiceDisplayName("Měsíc")]
            Month,

            [Devmasters.Enums.NiceDisplayName("Rok")]
            Year
        }

        private Options options;

        public class Options
        {
            public string[] Exclude { get; set; } = new string[] { };
            public IntervalEnum Interval { get; set; }
        }

        public DatasetyStatistika(Options options)
        {
            this.options = options;
        }


        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = statistiky.Get();
                System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
                sb.Append("<table class='table table-new table-dotted table-responsive'>");
                sb.Append(
                    $"<thead><tr><td>dataset</td><td>celkem zaznamu</td><td>za {options.Interval.ToNiceDisplayName()}</td><td>posl.změna</td></tr></thead>");
                sb.Append("<tbody>");
                foreach (var ds in data
                             .Where(ds => options.Exclude.Contains(ds.Dataset.DatasetId) == false)
                             .OrderByDescending(o => o.PosledniZmena)
                        )
                {
                    sb.Append("<tr>");
                    sb.Append(
                        $"<td>{ds.Dataset.DatasetId}</td><td>{ds.PocetCelkem}</td><td>{ds.PocetZaInterval[options.Interval]}</td><td>{ds.PosledniZmena:dd.MM.yy}</td>");
                    sb.Append("</tr>");
                }

                sb.Append("</tbody></table>");

                return Task.FromResult(HealthCheckResult.Healthy(sb.ToString()));
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Unknown status, cannot read data from network disk",
                    e));
            }
        }
    }
}