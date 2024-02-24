extern alias IPNetwork2;

using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class IISConnections : IHealthCheck
    {
        public Options options { get; }

        public IISConnections(Options filterOptions)
        {
            this.options = filterOptions;
        }

        public class Options
        {
            public string AppPoolNameFilter { get; set; }
            public string StartsWithFilter { get; set; }
            public int? CountWarningThreshold { get; set; } = null;
            public int? CountErrorThreshold { get; set; } = null;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Dictionary<string, Request[]> requests = new Dictionary<string, Request[]>();

            try
            {
                requests = GetIIS(options.AppPoolNameFilter);
                IEnumerable<Request> allReq = requests.Values.SelectMany(m => m);
                int max = 0;
                float median = 0;
                if (allReq.Count() > 0)
                {
                    max = allReq.Max(m => m.TimeElapsed);
                    median = MathNet.Numerics.Statistics.Statistics.Median(allReq.Select(m => (float)m.TimeElapsed));
                }
                string report = $"IIS requests:{allReq.Count()}<br/>\n"
                    + $"Max TimeElapsed:{max} ms<br/>\n"
                    + $"Median TimeElapsed:{median}";
                var data = new Dictionary<string, object>() {
                    {"Count",allReq.Count()},
                    {"Max_TimeElapsed",max },
                    {"Median_TimeElapsed",median },
                };
                if (options.CountErrorThreshold.HasValue && allReq.Count() >= options.CountErrorThreshold.Value)
                    return Task.FromResult(HealthCheckResult.Unhealthy(description: report, data: data));
                if (options.CountWarningThreshold.HasValue && allReq.Count() >= options.CountWarningThreshold.Value)
                    return Task.FromResult(HealthCheckResult.Degraded(description: report, data: data));
                else
                    return Task.FromResult(HealthCheckResult.Healthy(description: report, data: data));
            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Degraded("Cannot read data from system API", e));
            }


        }


        public static Dictionary<string, Request[]> GetIIS(string appPoolNameFilter = null, int timeElapsedFilter = 1000)
        {

            using (ServerManager manager = new ServerManager())
            {
                try
                {

                    var pools = manager.ApplicationPools.ToArray();
                    //.Select(p=>p);
                    Dictionary<string, Request[]> requests = pools
                        .Where(m=>m.Name == appPoolNameFilter || appPoolNameFilter == null)
                        //.Where(m=>m.Name=="ladmin.hlidacstatu.cz")
                        .SelectMany(pool => pool.WorkerProcesses)
                        .SelectMany(wp => wp.GetRequests(timeElapsedFilter))
                        .Select(req => new { site = req.HostName, req = req })
                        .GroupBy(k => k.site, v => v.req, (k, v) => new KeyValuePair<string, Request[]>(k, v.ToArray()))
                        .ToDictionary(k => k.Key, v => v.Value);


                    return requests;
                }
                catch (Exception ex)
                {
                    return new Dictionary<string, Request[]>() { { "no data", new Request[] { } } };
                }
                finally
                {

                }
            }

        }

    }
}


