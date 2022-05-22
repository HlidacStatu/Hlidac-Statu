using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using HlidacStatu.Entities;

using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.EntityFrameworkCore;

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
        public static void SaveAlert(int serverId, Alert.AlertStatus status)
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

        public static List<UptimeServer> GetServersToCheck(int numOfServers = 30, bool debug = false)
        {
            try
            {
                List<UptimeServer> list = null;
                using (DbEntities db = new DbEntities())
                {
                    if (debug)
                    {
                        list = db.UptimeServers
                            .AsNoTracking()
                            .Where(m => m.Id == 230)
                            .ToList();
                    }
                    else
                        list = db.UptimeServers.FromSqlInterpolated($"exec GetUptimeServers {numOfServers}")
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

        public const string NotInGroup = "other";
        public static IEnumerable<int> ServersIn(string group)
        {
            int[] serverIds = null;
            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
            {
                var servers = AllActiveServers();
                if (group == NotInGroup)
                {
                    serverIds = servers
                        .Where(m => m.GroupArray()?.Count() == 0)
                        .Select(m => m.Id)
                        .ToArray();
                }
                else if (Devmasters.TextUtil.IsNumeric(group))
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
        static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<UptimeServer.HostAvailability>> _allActiveServers24hoursStatsCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<UptimeServer.HostAvailability>>(TimeSpan.FromMinutes(20), "_allActiveStatniWebyServersStat",
                (o) =>
                {
                    List<UptimeServer.HostAvailability> res = AvailabilityForDayByIds(AllActiveServers().Select(m=>m.Id))
                        .ToList();
                    return res;
                }
        );

        static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<UptimeServer.HostAvailability>> _allActiveServersWeekStatsCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<UptimeServer.HostAvailability>>(TimeSpan.FromHours(6), "_allActiveStatniWebyServersStatWeek",
                (o) =>
                {
                    var res = AllActiveServers()
                        .AsParallel()
                        .Select(m => AvailabilityForWeekById(m.Id))
                        .ToList();
                    return res;
                }
        );

        static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer[]> _allActiveServersCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer[]>(TimeSpan.FromMinutes(10), "_allActiveStatniWebyServers",
                (o) =>
            {
                using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
                    return db.UptimeServers
                        .AsNoTracking()
                        .Where(m => m.Active == 1)
                        .ToArray();
            }
        );
        public static List<UptimeServer.HostAvailability> AllActiveServers24hoursStat()
        {
            return _allActiveServers24hoursStatsCache.Get();
        }

        public static List<UptimeServer.HostAvailability> AllActiveServersWeekStat()
        {
            return _allActiveServersWeekStatsCache.Get();
        }

        public static UptimeServer[] AllActiveServers()
        {
            return _allActiveServersCache.Get();
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

        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForDayByIds(IEnumerable<int> serverIds)
        {
            if (serverIds?.Count() == null)
                return null;
            if (serverIds.Count() == 0)
                return null;

            UptimeServer.HostAvailability[] allData = uptimeServersCache1Day.Get();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData
                .Where(m => serverIds.Contains(m.Host.Id))
                .OrderByDescending(o => o.Host.Name)
                .ToList();
            return choosen;
        }
        public static UptimeServer.HostAvailability AvailabilityForWeekById(int serverId)
        {
            return AvailabilityForWeekByIds(new int[] { serverId }).FirstOrDefault();
        }
        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForWeekByIds(IEnumerable<int> serverIds)
        {
            if (serverIds?.Count() == null)
                return null;
            if (serverIds.Count() == 0)
                return null;

            UptimeServer.HostAvailability[] allData = uptimeServersCache7Days.Get();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData
                .Where(m => serverIds.Contains(m.Host.Id))
                .OrderByDescending(o => o.Host.Name)
                .ToList();
            return choosen;
        }


        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]> uptimeServersCache1Day =
      new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(2),
          (obj) =>
          {
              var res = _availability(24);
              return res.ToArray();
          });

        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]> uptimeServersCache7Days
       = new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(30),
           "uptimeServersCache7Days",
          (id) =>
          {
              var res = _availability(7 * 24);
              return res.ToArray();
          }
          );


        private static IEnumerable<UptimeServer.HostAvailability> _availability(int hoursBack)
        {
            int[] serverIds = AllActiveServers().Select(m => m.Id).ToArray();
            return _availability(serverIds, hoursBack);
        }

        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, int hoursBack)
        {
            return _availability(serverIds, TimeSpan.FromHours(hoursBack));
        }
        public static IEnumerable<UptimeServer.HostAvailability> GetAvailabilityNoCache(TimeSpan intervalBack, params int[] serverIds)
        {
            return _availability(serverIds, intervalBack);
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
                .OrderBy(o => o.Host.Name)
                .ToArray()
                ;


            return zabList;
        }
    }
}