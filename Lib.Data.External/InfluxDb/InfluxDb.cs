using Devmasters.Collections;
using InfluxDB.Client;
using InfluxDB.Client.Writes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Lib.Data.External
{

    public static class InfluxDb
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(InfluxDb));

        public class ResponseItem
        {
            public int ServerId { get; set; }
            public DateTime CheckStart { get; set; }
            public long ResponseCode { get; set; }
            public long ResponseSize { get; set; }
            public long ResponseTimeInMs { get; set; }
        }

        static object lockObj = new object();
        static InfluxDBClient influxDbClient = null;
        static InfluxDb()
        {
            if (influxDbClient == null)
            {
                lock (lockObj)
                {
                    if (influxDbClient == null)
                    {
                        var options = new InfluxDBClientOptions.Builder()
                            .Url(Devmasters.Config.GetWebConfigValue("InfluxDb"))
                            .AuthenticateToken(Devmasters.Config.GetWebConfigValue("InfluxDbToken"))
                            //.ReadWriteTimeOut(TimeSpan.FromMinutes(10))
                            .TimeOut(TimeSpan.FromMinutes(10))
                            .Build();

                        influxDbClient = InfluxDBClientFactory.Create(options);

                    }
                }
            }
        }
        
        public static bool AddPoints(string bucketName, string orgName, params PointData[] points)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            if (points.Length == 0)
                return true;

            try
            {
                using (var writeApi = influxDbClient.GetWriteApi())
                {
                    foreach (var point in points)
                    {
                        writeApi.WritePoint(point, bucketName, orgName);

                    }
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "UptimeServerRepo.SaveLastCheck error ");
                return false;
            }
        }
        
        public static async Task<IEnumerable<ResponseItem>> GetAvailbilityAsync(int[] serverIds, TimeSpan timeBack)
        {
            var sTimeBack = "";
            if (timeBack.TotalHours >= 1)
                sTimeBack = $"-{timeBack.TotalHours:F0}h";
            else
                sTimeBack = $"-{timeBack.TotalMinutes:F0}m";

            string query = "";
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> fluxTables = new ();

            var serverParts = serverIds.Split(50);

            foreach (var serverPart in serverParts)
            {

                query = "from(bucket:\"uptimer\") \n"
                    + $" |> range(start: {sTimeBack}) \n"
                    + "  |> filter(fn: (r) => r[\"_measurement\"] == \"uptime\") \n"
                    + "  |> filter(fn: (r) => " + string.Join(" or ", serverPart.Select(m => $"r[\"serverid\"] == \"{m}\"")) + " ) \n"
                    + "  |> filter(fn: (r) => r[\"fieldname\"] == \"responseCode\" or r[\"fieldname\"] == \"responseSize\" or r[\"fieldname\"] == \"responseTime\")  \n"
                ;

                if (false && timeBack.TotalHours > 25)
                    query = query + "\n"
                        + "|> aggregateWindow(every: 10m, fn: max, createEmpty: false)"
                      + "|> yield(name: \"max\")"
                      + "|> duplicate(column: \"_stop\", as: \"_time\")";

                if (false) //another grouping
                    query = query + "\n"
                        + @"  |> filter(fn: (r) => r[""fieldname""] == ""responseTime"")
                          |> aggregateWindow(every: 10m,         
                              fn: (column, tables=<-) => tables |> quantile(q: 0.95, method: ""exact_selector""),
                              //fn: max,
                              createEmpty: false)
                          |> yield(name: ""max"")
                          |> duplicate(column: ""_stop"", as: ""_time"")
                        ";

                try
                {
                    _logger.Debug("Calling InfluxDB query {query} from time {timeBack}", query, timeBack);

                    List<InfluxDB.Client.Core.Flux.Domain.FluxTable> res =
                        await influxDbClient.GetQueryApi().QueryAsync(query, "hlidac");

                    fluxTables.AddRange(res);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "InfluxDB query error\n{query}", query);
                    //throw;
                }
            }
            var allData = fluxTables.SelectMany(m => m.Records)
                .Select(m => new
                {
                    serverId = Convert.ToInt32(m.Values["serverid"] as string),
                    fieldname = m.Values["fieldname"] as string,
                    value = Convert.ToInt64(m.Values["_value"]),
                    time = ((NodaTime.Instant)m.Values["_time"]).ToDateTimeUtc().ToLocalTime()
                })
                .GroupBy(k => new { s = k.serverId, t = k.time }, v => v);

            var items = allData
                .Select(i => new ResponseItem()
                {
                    ServerId = i.Key.s,
                    CheckStart = i.Key.t,
                    ResponseCode = i.Where(m => m.fieldname == "responseCode").FirstOrDefault()?.value ?? -1,
                    ResponseSize = i.Where(m => m.fieldname == "responseSize").FirstOrDefault()?.value ?? -1,
                    ResponseTimeInMs = i.Where(m => m.fieldname == "responseTime").FirstOrDefault()?.value ?? -1,
                }
                )
                .ToArray();

            return items;
        }
    }
}