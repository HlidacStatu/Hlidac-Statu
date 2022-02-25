using HlidacStatu.Entities;
using HlidacStatu.Util.Cache;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class UptimeServerRepo
    {



        static InfluxDBClient influxDbClient = InfluxDBClientFactory.Create(
            Devmasters.Config.GetWebConfigValue("InfluxDb"),
            Devmasters.Config.GetWebConfigValue("InfluxDbToken"));


        public static string PatriPodUradJmeno(this UptimeServer server)
        {
            if (string.IsNullOrEmpty(server.ICO))
                return string.Empty;
            else
                return Firmy.GetJmeno(server.ICO);
        }

        public static void SaveLastCheck(UptimeItem lastCheck, UptimeServer uptimeServerTrigger)
        {
            bool triggerScreenshot = false;
            bool triggerAlert = false;



            using (DbEntities db = new DbEntities())
            {
                var server = Load(uptimeServerTrigger.Id);
                server.LastCheck = lastCheck.CheckStart;
                server.LastResponseCode = lastCheck.ResponseCode;
                server.LastResponseSize = lastCheck.ResponseSize;
                server.LastResponseTimeInMs = lastCheck.ResponseTimeInMs;
                server.TakenByUptimer = null;

                db.UptimeServers.Attach(server);
                if (server.Id==0)
                    db.Entry(server).State = EntityState.Added;
                else
                    db.Entry(server).State = EntityState.Modified;

                try
                {

                    db.SaveChanges();

                    using (var writeApi = influxDbClient.GetWriteApi())
                    {
                        var point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId.ToString())
                            .Tag("fieldname", "responseTime")
                            .Field("value", lastCheck.ResponseTimeInMs)
                            .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S);
                        writeApi.WritePoint("uptimer", "hlidac", point);

                        point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId.ToString())
                            .Tag("fieldname", "responseSize")
                            .Field("value", lastCheck.ResponseSize)
                            .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S);
                        writeApi.WritePoint("uptimer", "hlidac", point);

                        point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId.ToString())
                            .Tag("fieldname", "responseCode")
                            .Field("value", (long)lastCheck.ResponseCode)
                            .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S);
                        writeApi.WritePoint("uptimer", "hlidac", point);

                    }
                }
                catch (System.Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Error("UptimeServerRepo.SaveLastCheck error ", e);
                    throw;
                }

            }
        }
        public static UptimeServer Load(int serverId)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.UptimeServers
                    .AsNoTracking()
                    .FirstOrDefault(m => m.Id == serverId);
            }
        }


        public static void Save(UptimeServer uptimeServer)
        {
            using (DbEntities db = new DbEntities())
            {
                db.UptimeServers.Attach(uptimeServer);
                if (uptimeServer.Id == 0)
                {
                    db.Entry(uptimeServer).State = EntityState.Added;                    
                }
                else
                    db.Entry(uptimeServer).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public static List<UptimeServer> GetServersToCheck(int numOfServers = 30)
        {
            try
            {

                using (DbEntities db = new DbEntities())
                {
                    var list = db.UptimeServers.FromSqlInterpolated($"exec GetUptimeServers {numOfServers}")
                        .AsNoTracking()
                        .ToList();

                    return list;
                }
            }
            catch (System.Exception e)
            {
                return new List<UptimeServer>();
            }


        }

        public static IEnumerable<int> ServersIn(string group)
        {
            int[] serverIds = null;
            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
            {
                var servers = AllActiveServers(db);
                if (Devmasters.TextUtil.IsNumeric(group))
                    serverIds = servers
                        .Where(m => m.Priorita == Convert.ToInt32(group))
                        .Select(m => m.Id)
                        .ToArray();
                else if (!string.IsNullOrEmpty(group))
                    serverIds = servers
                        .Where(m => m.GroupArray().Contains(group) == true)
                        .Select(m => m.Id)
                        .ToArray();
                else
                    serverIds = servers
                        .Select(m => m.Id)
                        .ToArray();
            }
            return serverIds;
        }


        public static UptimeServer[] AllActiveServers(Entities.DbEntities existingConn = null)
        {
            if (existingConn != null)
                return existingConn.UptimeServers.AsNoTracking().ToArray();


            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
                return db.UptimeServers
                    .AsNoTracking()
                    .Where(m=> m.Active == 1)
                    .ToArray();

        }

        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForDayByGroup(string group)
        {
            int[] serverIds = ServersIn(group).ToArray();
            return AvailabilityForDayByIds(serverIds);
        }
        public static UptimeServer.HostAvailability AvailabilityForDayById(int serverId)
        {
                return AvailabilityForDayByIds(new int[] { serverId }).FirstOrDefault();
            
        }
        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForDayByIds(int[] serverIds)
        {
            if (serverIds?.Length == null)
                return null;
            if (serverIds.Length == 0)
                return null;

            UptimeServer.HostAvailability[] allData = uptimeServersCache1Day.Get();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData.Where(m => serverIds.Contains(m.Host.Id)).ToList();
            return choosen;
        }

        public static UptimeServer.HostAvailability AvailabilityForWeekById(int serverId)
        {
            if (serverId==0)
                return null;

            return uptimeServerCache7Day.Get(serverId);
        }


        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]> uptimeServersCache1Day =
      new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(1),
          (obj) =>
          {
              var res = _availability(24);
              return res.ToArray();
          });

        private static AutoUpdateMemoryCacheManager<UptimeServer.HostAvailability, int> uptimeServerCache7Day
       = AutoUpdateMemoryCacheManager<UptimeServer.HostAvailability, int>.GetSafeInstance("uptimeServerCache7Day",
          (id) => {
              var res = _availability(new int[] { id }, 7 * 24);
              return res.FirstOrDefault();
          }
           ,TimeSpan.FromMinutes(30));


        private static IEnumerable<UptimeServer.HostAvailability> _availability(int hoursBack)
        {
            int[] serverIds = AllActiveServers().Select(m => m.Id).ToArray();
            return _availability(serverIds, hoursBack);
        }
        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, int hoursBack)
        {
            UptimeServer[] allServers = AllActiveServers();


            string query = "";
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> fluxTables = null;

            query = "from(bucket:\"uptimer\")"
                + $" |> range(start: -{hoursBack}h)"
                + "  |> filter(fn: (r) => r[\"_measurement\"] == \"uptime\")"
                + "  |> filter(fn: (r) => " + serverIds.Select(m => $"r[\"serverid\"] == \"{m}\"").Aggregate((f, s) => f + " or " + s) + " )"
                + "  |> filter(fn: (r) => r[\"_field\"] == \"value\")"
            ;
            if (false && hoursBack > 25)
                query = query + "\n"
                    + "|> aggregateWindow(every: 10m, fn: max, createEmpty: false)"
                  + "|> yield(name: \"max\")"
                  + "|> duplicate(column: \"_stop\", as: \"_time\")";

            fluxTables = influxDbClient.GetQueryApi().QueryAsync(query, "hlidac").Result;
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
                .Select(i => new
                {
                    ServerId = i.Key.s,
                    Server = allServers.First(m => m.Id == i.Key.s),
                    CheckStart = i.Key.t,
                    ResponseCode = i.Where(m => m.fieldname == "responseCode").FirstOrDefault()?.value ?? -1,
                    ResponseSize = i.Where(m => m.fieldname == "responseSize").FirstOrDefault()?.value ?? -1,
                    ResponseTimeInMs = i.Where(m => m.fieldname == "responseTime").FirstOrDefault()?.value ?? -1,
                }
                )
                .ToArray();

            var zabList = items
                .GroupBy(k => k.ServerId, v => v)
                .Select(g => new UptimeServer.HostAvailability(
                                g.FirstOrDefault()?.Server
                                ,
                                g.OrderBy(m => m.CheckStart)
                                .Select(m => new UptimeServer.UptimeMeasure()
                                {
                                    clock = m.CheckStart,
                                    itemId = g.Key,
                                    value = m.ResponseCode >= 400 ? UptimeServer.Availability.BadHttpCode : (m.ResponseTimeInMs > 15000 ? UptimeServer.Availability.TimeOuted2 : ((decimal)m.ResponseTimeInMs) / 1000m)
                                }
                                )
                            ) //zabhost
                    );

            return zabList;
        }
    }
}