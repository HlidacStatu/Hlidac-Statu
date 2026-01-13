using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;

using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;

using Microsoft.EntityFrameworkCore;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class UptimeServerRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(UptimeServerRepo));
        public static string PatriPodUradJmeno(this UptimeServer server)
        {
            if (string.IsNullOrEmpty(server.ICO))
                return string.Empty;
            else
                return Firmy.GetJmeno(server.ICO);
        }
        public static void SaveAlert(int serverId, Alert.AlertStatus status)
        {
            HlidacStatu.Connectors.DirectDB.Instance.NoResult("exec UptimeServer_Savealert @serverId, @lastAlertedStatus, @lastAlertSent ",
                new IDataParameter[]
                {
                    new SqlParameter("serverId", serverId),
                    new SqlParameter("lastAlertedStatus", (int)status),
                    new SqlParameter("lastAlertSent", DateTime.Now),
                });

        }


        public static Task<IEnumerable<UptimeServer.HostAvailability>> DirectShortAvailabilityAsync(int[] serverIds, TimeSpan intervalBack)
        {
            if (serverIds?.Length == null)
                return Task.FromResult<IEnumerable<UptimeServer.HostAvailability>>(null);
            if (serverIds.Length == 0)
                return Task.FromResult<IEnumerable<UptimeServer.HostAvailability>>(null);

            return DirectAvailabilityFromInFluxAsync(serverIds, intervalBack);
        }
        public static Task<IEnumerable<UptimeServer.HostAvailability>> GetAvailabilityNoCacheAsync(TimeSpan intervalBack, params int[] serverIds)
        {
            return DirectAvailabilityFromInFluxAsync(serverIds, intervalBack);
        }

        private static async Task<IEnumerable<UptimeServer.HostAvailability>> DirectAvailabilityFromInFluxAsync(int[] serverIds, TimeSpan intervalBack)
        {
            UptimeServer[] allServers = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers();

            var items = await HlidacStatu.Lib.Data.External.InfluxDb.GetAvailbilityAsync(serverIds, intervalBack);

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

        public static async Task SaveLastCheckAsync(UptimeItem lastCheck)
        {
            Devmasters.DT.StopWatchLaps swl = new Devmasters.DT.StopWatchLaps();

            try
            {
                var lap = swl.StopPreviousAndStartNextLap("SQL save");
                HlidacStatu.Connectors.DirectDB.Instance.NoResult("exec UptimeServer_SaveStatus @serverId, @lastCheck, @lastResponseCode, @lastResponseSize, @lastResponseTimeInMs, @lastUptimeStatus",
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

                lap = swl.StopPreviousAndStartNextLap("InfluxDb save");
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

                lap = swl.StopPreviousAndStartNextLap("CheckAlert ");
                await UptimeServerRepo.Alert.CheckAndAlertServerAsync(lastCheck.ServerId);
                lap.Stop();

                Console.WriteLine("Times:\n" + swl.ToString());
            }
            catch (System.Exception e)
            {
                Console.WriteLine("Times:\n" + swl.ToString());

                _logger.Error(e, "UptimeServerRepo.SaveLastCheck error ");
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
        
        public static async Task<List<UptimeServer>> AllUptimeServers()
        {
            await using var db = new DbEntities();
            return await db.UptimeServers.ToListAsync();
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
                    uptimeServer.Created = DateTime.Now;
                    db.Entry(uptimeServer).State = EntityState.Added;
                }
                else
                    db.Entry(uptimeServer).State = EntityState.Modified;

                db.SaveChanges();
            }
        }

        public static async Task<List<UptimeServer>> GetServersToCheckAsync(int numOfServers = 30, bool debug = false)
        {
            try
            {
                List<UptimeServer> list = null;
                await using (DbEntities db = new DbEntities())
                {
                    if (debug)
                    {
                        list = await db.UptimeServers
                            .AsNoTracking()
                            .Where(m => m.Id == 440)
                            .ToListAsync();
                    }
                    else
                        list = await db.UptimeServers.FromSqlInterpolated($"exec GetUptimeServers {numOfServers}")
                        .AsNoTracking()
                        .ToListAsync();

                    return list;
                }
            }
            catch (Exception)
            {
                return new List<UptimeServer>();
            }


        }

        public static IEnumerable<int> ServersIn(string group)
        {
            int[] serverIds = null;
            using (Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
            {
                var servers = AllActiveServers();
                if (group == UptimeServer.NotInGroup)
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



        static Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer[]> _allActiveServersCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer[]>(TimeSpan.FromMinutes(10), "_allActiveStatniWebyServers",
                (o) =>
                {
                    using (HlidacStatu.Entities.DbEntities db = new HlidacStatu.Entities.DbEntities())
                    {
                        try
                        {

                        var ret = db.UptimeServers
                            .AsNoTracking()
                            .Where(m => m.Active == 1)
                            .ToArray();

                        return ret;
                        }
                        catch (Exception e)
                        {

                            throw;
                        }
                    }
                }
        );
        public static UptimeServer[] AllActiveServers()
        {
            return _allActiveServersCache.Get();
        }

        public static async Task DeleteAsync(UptimeServer uptimeServer)
        {
            if (uptimeServer is null)
                return;
        
            await using var dbContext = new DbEntities();
            dbContext.UptimeServers.Remove(uptimeServer);
            await dbContext.SaveChangesAsync();        }
    }
}