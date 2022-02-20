using HlidacStatu.Entities;

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
                if (string.IsNullOrEmpty(server.Id))
                    db.Entry(server).State = EntityState.Added;
                else
                    db.Entry(server).State = EntityState.Modified;

                try
                {

                    db.SaveChanges();

                    //Repositories.ES.Manager.GetESClient_Uptime().Index<UptimeItem>(lastCheck, m => m.Id(lastCheck.Id));
                    using (var writeApi = influxDbClient.GetWriteApi())
                    {
                        var point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId)
                            .Tag("fieldname", "responseTime")
                            .Field("value", lastCheck.ResponseTimeInMs)
                            .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S);
                        writeApi.WritePoint("uptimer", "hlidac", point);

                        point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId)
                            .Tag("fieldname", "responseSize")
                            .Field("value", lastCheck.ResponseSize)
                            .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S);
                        writeApi.WritePoint("uptimer", "hlidac", point);

                        point = PointData.Measurement("uptime")
                            .Tag("uptimer", lastCheck.Uptimer)
                            .Tag("serverid", lastCheck.ServerId)
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
        public static UptimeServer Load(string serverId)
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
                if (string.IsNullOrEmpty(uptimeServer.Id))
                    db.Entry(uptimeServer).State = EntityState.Added;
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

        public static IEnumerable<string> ServersIn(string group)
        {
            string[] serverIds = null;
            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
            {
                var servers = AllServers(db);
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


        public static UptimeServer[] AllServers(Entities.DbEntities existingConn = null)
        {
            if (existingConn != null)
                return existingConn.UptimeServers.AsNoTracking().ToArray();


            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
                return db.UptimeServers.AsNoTracking().ToArray();

        }

        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityByGroup(string group, int hoursBack)
        {
            string[] serverIds = ServersIn(group).ToArray();
            return AvailabilityByIds(serverIds, hoursBack);
        }
        public static UptimeServer.HostAvailability AvailabilityById(string serverId, int hoursBack)
        {
            return AvailabilityByIds(new string[] { serverId }, hoursBack).FirstOrDefault();
        }
        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityByIds(string[] serverIds, int hoursBack)
        {
            if (serverIds?.Length == null)
                return null;
            if (serverIds.Length == 0)
                return null;

            UptimeServer.HostAvailability[] allData = null;
            if (hoursBack<=25)
                allData = uptimeServersCache1Day.Get();
            else
                allData = uptimeServersCache7Day.Get();
            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData.Where(m => serverIds.Contains(m.Host.Id)).ToList();
            return choosen;
        }



        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]> uptimeServersCache1Day =
      new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(1),
          (obj) =>
          {
              var res = _availability(24);
              return res.ToArray();
          });
        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]> uptimeServersCache7Day =
      new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(30),
          (obj) =>
          {
              var res = _availability(7*24);
              return res.ToArray();
          });


        private static IEnumerable<UptimeServer.HostAvailability> _availability(int hoursBack)
        {
            string[] serverIds = AllServers().Select(m=>m.Id).ToArray();


            UptimeServer[] allServers = AllServers();

            string query = "";
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> fluxTables = null;

            query = "from(bucket:\"uptimer\")"
                + $" |> range(start: -{hoursBack}h)"
                + "  |> filter(fn: (r) => r[\"_measurement\"] == \"uptime\")"
                + "  |> filter(fn: (r) => " + serverIds.Select(m => $"r[\"serverid\"] == \"{m}\"").Aggregate((f, s) => f + " or " + s) + " )"
                + "  |> filter(fn: (r) => r[\"_field\"] == \"value\")"
            ;
            if (hoursBack > 25)
                query = query + "\n"
                    + "|> aggregateWindow(every: 10m, fn: max, createEmpty: false)"
                  + "|> yield(name: \"max\")"
                  + "|> duplicate(column: \"_stop\", as: \"_time\")";

            fluxTables = influxDbClient.GetQueryApi().QueryAsync(query, "hlidac").Result;
            var allData = fluxTables.SelectMany(m => m.Records)
                .Select(m => new
                {
                    serverId = m.Values["serverid"] as string,
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