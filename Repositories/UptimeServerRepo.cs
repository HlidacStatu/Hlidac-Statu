using HlidacStatu.Entities;
using HlidacStatu.Lib.Data.External.Zabbix;

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

                    Repositories.ES.Manager.GetESClient_Uptime().Index<UptimeItem>(lastCheck, m => m.Id(lastCheck.Id));
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


        public static IEnumerable<ZabHostAvailability> Availability(string group, int hoursBack)
        {
            string[] serverIds = null;
            UptimeServer[] servers = null;
            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
            {
                servers = db.UptimeServers
                    .AsNoTracking()
                    .ToArray();
                if (Devmasters.TextUtil.IsNumeric(group))
                    serverIds = servers
                        .Where(m => m.Priorita == Convert.ToInt32(group))
                        .Select(m => m.Id)
                        .ToArray();
                else
                    serverIds = servers
                        .Where(m => m.GroupArray().Contains(group) == true)
                        .Select(m => m.Id)
                        .ToArray();
            }
            string query = "";
            List<InfluxDB.Client.Core.Flux.Domain.FluxTable> fluxTables = null;
            if (serverIds?.Length == null)
                return null;

            query = "from(bucket:\"uptimer\")"
                + $" |> range(start: -{hoursBack}h)"
                + "  |> filter(fn: (r) => r[\"_measurement\"] == \"uptime\")"
                + "  |> filter(fn: (r) => " + serverIds.Select(m => $"r[\"serverid\"] == \"{m}\"").Aggregate((f, s) => f + " or " + s) + " )"
                + "  |> filter(fn: (r) => r[\"_field\"] == \"value\")"
            ;

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
                    Server = servers.First(m => m.Id == i.Key.s),
                    CheckStart = i.Key.t,
                    ResponseCode = i.Where(m => m.fieldname == "responseCode").FirstOrDefault()?.value ?? -1,
                    ResponseSize = i.Where(m => m.fieldname == "responseSize").FirstOrDefault()?.value ?? -1,
                    ResponseTimeInMs = i.Where(m => m.fieldname == "responseTime").FirstOrDefault()?.value ?? -1,
                }
                )
                .ToArray();

            var zabList = items
                .GroupBy(k => k.ServerId, v => v)
                .Select(g => new ZabHostAvailability(
                    new ZabHost(g.Key, g.FirstOrDefault()?.Server.Host(), g.FirstOrDefault()?.Server.PublicUrl, g.FirstOrDefault()?.Server.Description, g.FirstOrDefault()?.Server.GroupArray())
                                ,
                                g.OrderBy(m => m.CheckStart)
                                .Select(m => new ZabHistoryItem()
                                {
                                    clock = m.CheckStart,
                                    itemId = g.Key,
                                    value = m.ResponseCode >= 400 ? ZabAvailability.BadHttpCode : (m.ResponseTimeInMs > 15000 ? ZabAvailability.TimeOuted2 : ((decimal)m.ResponseTimeInMs)/1000m)
                                }
                                )
                            ) //zabhost
                    );

            return zabList;
        }

    }
}