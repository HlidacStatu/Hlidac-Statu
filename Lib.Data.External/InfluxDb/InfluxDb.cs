using System;
using System.Collections.Generic;
using System.Linq;

using InfluxDB.Client;
using InfluxDB.Client.Writes;

namespace HlidacStatu.Lib.Data.External
{

    public static class InfluxDb
    {
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
                HlidacStatu.Util.Consts.Logger.Error("UptimeServerRepo.SaveLastCheck error ", e);
                return false;
            }


        }


        public static IEnumerable<ResponseItem> GetAvailbility(int[] serverIds, TimeSpan timeBack)
        {
            var sTimeBack = "";
            if (timeBack.TotalHours >= 1)
                sTimeBack = $"-{timeBack.TotalHours:F0}h";
            else
                sTimeBack = $"-{timeBack.TotalMinutes:F0}m";

            string query = "";
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> fluxTables = null;

            query = "from(bucket:\"uptimer\")"
                + $" |> range(start: {sTimeBack})"
                + "  |> filter(fn: (r) => r[\"_measurement\"] == \"uptime\")"
                + "  |> filter(fn: (r) => " + serverIds.Select(m => $"r[\"serverid\"] == \"{m}\"").Aggregate((f, s) => f + " or " + s) + " )"
                + "  |> filter(fn: (r) => r[\"_field\"] == \"value\")"
            ;
            if (false && timeBack.TotalHours > 25)
                query = query + "\n"
                    + "|> aggregateWindow(every: 10m, fn: max, createEmpty: false)"
                  + "|> yield(name: \"max\")"
                  + "|> duplicate(column: \"_stop\", as: \"_time\")";


            try
            {
                fluxTables = influxDbClient.GetQueryApi().QueryAsync(query, "hlidac")
                    .ConfigureAwait(false).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {

                throw;
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