
using Devmasters.Enums;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class DatasetZpracovane : IHealthCheck
    {

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
            public string DatasetId { get; set; }
            public int MinRecordsInInterval { get; set; }
            public IntervalEnum Interval { get; set; }
        }

        public DatasetZpracovane(Options options)
        {
            this.options = options;
        }


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            try
            {
                string result = "";
                bool bad = false;
                DateTime dateFrom = DateTime.Now.Date;
                DateTime dateTo = DateTime.Now.Date;
                string dateQ = "";
                switch (options.Interval)
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
                    default:
                        break;
                }

                var ds = Datasets.DataSet.CachedDatasets.Get(options.DatasetId);
                if (ds != null)
                {
                    var res = await Datasets.DatasetRepo.Searching.SearchDataRawAsync(ds, $"DbCreated:{dateQ}", 1, 1,
                        "0", exactNumOfResults: true);

                    result += $"{res.Total} zaznamů za {options.Interval.ToNiceDisplayName()} \n";
                    if (res.Total < options.MinRecordsInInterval)
                        bad = true;


                    if (bad)
                        return HealthCheckResult.Degraded(result);
                    else
                        return HealthCheckResult.Healthy(result);
                }
                return HealthCheckResult.Unhealthy($"Dataset {options.DatasetId} doesn't exists.");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Unknown status, cannot read data from network disk", e);
            }


        }




    }
}


