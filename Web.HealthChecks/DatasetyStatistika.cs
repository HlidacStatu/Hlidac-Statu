using Devmasters.Enums;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.Datasets;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Web.HealthChecks
{
    public class DatasetyStatistika : IHealthCheck
    {
        private readonly IFusionCache _cache =
            Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(DatasetyStatistika));

        private ValueTask<List<DatasetStat>> GetStatistikyCachedAsync() => _cache.GetOrSetAsync(
            $"_DatasetStat_all", async _ =>
            {
                try
                {
                    List<DatasetStat> res = new List<DatasetStat>();
                    foreach (var ds in await DataSetCache.GetProductionDatasetsAsync())
                    {
                        var item = new DatasetStat();
                        item.Dataset = ds;

                        var allrec = ds.SearchDataAsync("*", 1, 1, sort: "DbCreated desc", exactNumOfResults: true)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
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

                            var pocet = ds.SearchDataAsync(
                                    $"DbCreated:[{DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")} TO *]", 1, 0,
                                    exactNumOfResults: true)
                                .ConfigureAwait(false).GetAwaiter().GetResult();
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

        [ShowNiceDisplayName]
        public enum IntervalEnum
        {
            [NiceDisplayName("Den")]
            Day,

            [NiceDisplayName("Týden")]
            Week,

            [NiceDisplayName("Měsíc")]
            Month,

            [NiceDisplayName("Rok")]
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


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var data = await GetStatistikyCachedAsync();
                System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
                sb.Append("<table class='table table-sm table-borderless table-hover'>");
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

                return HealthCheckResult.Healthy(sb.ToString());
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Unknown status, cannot read data from network disk", e);
            }
        }
    }
}