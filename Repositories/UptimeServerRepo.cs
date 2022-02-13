using HlidacStatu.Entities;

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.EntityFrameworkCore;

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
    }
}