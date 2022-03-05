using HlidacStatu.Entities;
using HlidacStatu.Util.Cache;

using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class UptimeServerRepo
    {




        public static string PatriPodUradJmeno(this UptimeServer server)
        {
            if (string.IsNullOrEmpty(server.ICO))
                return string.Empty;
            else
                return Firmy.GetJmeno(server.ICO);
        }
        public static void SaveAlert(int serverId,Alert.AlertStatus status)
        {
            HlidacStatu.Connectors.DirectDB.NoResult("exec UptimeServer_Savealert @serverId, @lastAlertedStatus, @lastAlertSent ",
                new IDataParameter[]
                {
                    new SqlParameter("serverId", serverId),
                    new SqlParameter("lastAlertedStatus", (int)status),
                    new SqlParameter("lastAlertSent", DateTime.Now),
                });

        }
        public static void SaveLastCheck(UptimeItem lastCheck)
        {
            bool triggerScreenshot = false;
            bool triggerAlert = false;

            Devmasters.DT.StopWatchLaps swl = new Devmasters.DT.StopWatchLaps();

            try
            {
                var lap = swl.AddAndStartLap("SQL save");
                HlidacStatu.Connectors.DirectDB.NoResult("exec UptimeServer_SaveStatus @serverId, @lastCheck, @lastResponseCode, @lastResponseSize, @lastResponseTimeInMs, @lastUptimeStatus",
                    new IDataParameter[]
                    {
                        new SqlParameter("serverId", lastCheck.ServerId),
                        new SqlParameter("lastCheck", lastCheck.CheckStart),
                        new SqlParameter("lastResponseCode", lastCheck.ResponseCode),
                        new SqlParameter("lastResponseSize", lastCheck.ResponseSize),
                        new SqlParameter("lastResponseTimeInMs", lastCheck.ResponseTimeInMs),
                        new SqlParameter("lastUptimeStatus", lastCheck.ToAvailability().Status()),
                    });
                lap.Stop();

                lap = swl.AddAndStartLap("InfluxDb save");
                HlidacStatu.Lib.Data.External.InfluxDb.AddPoints("uptimer", "hlidac",
                    PointData.Measurement("uptime")
                        .Tag("uptimer", lastCheck.Uptimer)
                        .Tag("serverid", lastCheck.ServerId.ToString())
                        .Tag("fieldname", "responseTime")
                        .Field("value", lastCheck.ResponseTimeInMs)
                        .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S),
                    PointData.Measurement("uptime")
                        .Tag("uptimer", lastCheck.Uptimer)
                        .Tag("serverid", lastCheck.ServerId.ToString())
                        .Tag("fieldname", "responseSize")
                        .Field("value", lastCheck.ResponseSize)
                        .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S),
                    PointData.Measurement("uptime")
                        .Tag("uptimer", lastCheck.Uptimer)
                        .Tag("serverid", lastCheck.ServerId.ToString())
                        .Tag("fieldname", "responseCode")
                        .Field("value", (long)lastCheck.ResponseCode)
                        .Timestamp(lastCheck.CheckStart.ToUniversalTime(), WritePrecision.S)
                    );
                lap.Stop();

                lap = swl.AddAndStartLap("CheckAlert ");
                UptimeServerRepo.Alert.CheckAndAlertServer(lastCheck.ServerId);
                lap.Stop();

                Console.WriteLine("Times:\n" + swl.ToString());
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Times:\n" + swl.ToString());

                HlidacStatu.Util.Consts.Logger.Error("UptimeServerRepo.SaveLastCheck error ", e);
                throw;
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

        public static UptimeServer Load(Uri uri)
        {
            var url = uri.ToString();

            using (DbEntities db = new DbEntities())
            {
                var found = db.UptimeServers
                    .AsNoTracking()
                    .FirstOrDefault(m => m.PublicUrl == url);
                if (found == null)
                {
                    if (url.EndsWith("/"))
                        url = url.Substring(0, url.Length - 1);
                    else if (string.IsNullOrEmpty(uri.Query))
                        url = url + "/";

                    found = db.UptimeServers
                    .AsNoTracking()
                    .FirstOrDefault(m => m.PublicUrl == url);
                }
                if (found == null)
                {
                    if (uri.Scheme == "http")
                        url = url.Replace("http://", "https://");
                    else
                        url = url.Replace("https://", "http://");

                    found = db.UptimeServers
                    .AsNoTracking()
                    .FirstOrDefault(m => m.PublicUrl == url);
                }

                return found;
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
                    .Where(m => m.Active == 1)
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

        public static IEnumerable<UptimeServer.HostAvailability> ShortAvailability(int[] serverIds, TimeSpan intervalBack)
        {
            if (serverIds?.Length == null)
                return null;
            if (serverIds.Length == 0)
                return null;

            return _availability(serverIds, intervalBack);
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
            if (serverId == 0)
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
          (id) =>
          {
              var res = _availability(new int[] { id }, 7 * 24);
              return res.FirstOrDefault();
          }
           , TimeSpan.FromMinutes(30));


        private static IEnumerable<UptimeServer.HostAvailability> _availability(int hoursBack)
        {
            int[] serverIds = AllActiveServers().Select(m => m.Id).ToArray();
            return _availability(serverIds, hoursBack);
        }

        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, int hoursBack)
        {
            return _availability(serverIds, TimeSpan.FromHours(hoursBack));
        }

        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, TimeSpan intervalBack)
        {
            UptimeServer[] allServers = AllActiveServers();

            var items = HlidacStatu.Lib.Data.External.InfluxDb.GetAvailbility(serverIds, intervalBack);

            var zabList = items
                .GroupBy(k => k.ServerId, v => v)
                .Select(g => new UptimeServer.HostAvailability(
                                allServers.First(m => m.Id == g.First().ServerId),
                                g.OrderBy(m => m.CheckStart)
                                .Select(m => new UptimeServer.UptimeMeasure()
                                {
                                    clock = m.CheckStart,
                                    itemId = g.Key,
                                    value = m.ResponseCode >= 400 ? UptimeServer.Availability.BadHttpCode : (m.ResponseTimeInMs > 15000 ? UptimeServer.Availability.TimeOuted2 : ((decimal)m.ResponseTimeInMs) / 1000m)
                                }
                                )
                            ) //zabhost
                    )
                .ToArray()
                ;
            

            return zabList;
        }
    }
}