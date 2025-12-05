using HlidacStatu.Caching;
using HlidacStatu.Entities;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatuApi.Code
{
    public class Availability
    {
        private static readonly IFusionCache MemoryCache =
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(Availability));

        public static ValueTask<UptimeServer.HostAvailability[]> GetUptimeServer1dayCacheAsync() =>
            MemoryCache.GetOrSetAsync($"_uptimeServersCache1Day", async _ =>
                {
                    Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                    sw.Start();
                    var res = await AvailabilityAsync(24);
                    sw.Stop();
                    Log.ForContext<Availability>()
                        .Information("{action} updated of {part} in {duration} sec. Last record {date}", "updated",
                            "uptimeServersCache1Day", sw.Elapsed.TotalSeconds,
                            res.FirstOrDefault()?.Data?.Max(m => m.Time));
                    return res.ToArray();
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromMinutes(3))
            );

        public static ValueTask<UptimeServer.HostAvailability[]> GetUptimeServer7daysCacheAsync() =>
            MemoryCache.GetOrSetAsync($"_uptimeServersCache7Days", async _ =>
                {
                    Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                    sw.Start();
                    var res = await AvailabilityAsync(7 * 24);
                    sw.Stop();
                    Log.ForContext<Availability>().Information(
                        "{action} updated of {part} in {duration} sec. Last record {date}.", "updated",
                        "uptimeServersCache7Days", sw.Elapsed.TotalSeconds,
                        res.FirstOrDefault()?.Data?.Max(m => m.Time));
                    return res.ToArray();
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromMinutes(30))
            );


        private static Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityAsync(int hoursBack)
        {
            int[] serverIds = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers().Select(m => m.Id).ToArray();
            return AvailabilityAsync(serverIds, hoursBack);
        }

        private static Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityAsync(int[] serverIds,
            int hoursBack)
        {
            return AvailabilityAsync(serverIds, TimeSpan.FromHours(hoursBack));
        }

        private static async Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityAsync(int[] serverIds,
            TimeSpan intervalBack)
        {
            UptimeServer[] allServers = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers();


            IEnumerable<HlidacStatu.Lib.Data.External.InfluxDb.ResponseItem> items =
                new HlidacStatu.Lib.Data.External.InfluxDb.ResponseItem[] { };
            try
            {
                items = await HlidacStatu.Lib.Data.External.InfluxDb.GetAvailbilityAsync(serverIds, intervalBack);
            }
            catch (Exception e)
            {
                //var ex = e;
                Log.ForContext<Availability>().Error(e, "Cannot read data from InfluxDb");
            }

            var zabList = items
                    .GroupBy(k => k.ServerId, v => v)
                    .Select(g => new UptimeServer.HostAvailability(
                            allServers.First(m => m.Id == g.First().ServerId),
                            g.OrderBy(m => m.CheckStart)
                                .Select(m => new UptimeServer.UptimeMeasure()
                                    {
                                        clock = m.CheckStart,
                                        itemId = g.Key,
                                        value = m.ResponseCode >= 400
                                            ? UptimeServer.Availability.BadHttpCode
                                            : (m.ResponseTimeInMs > 15000
                                                ? UptimeServer.Availability.TimeOuted2
                                                : ((decimal)m.ResponseTimeInMs) / 1000m)
                                    }
                                )
                        ) //zabhost
                    )
                    .OrderBy(o => o.Host.Name)
                    .ToArray()
                ;


            return zabList;
        }

        public static async Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityForDayByIdsAsync(IEnumerable<int> serverIds)
        {
            if (serverIds?.Count() == null)
                return null;
            if (serverIds.Count() == 0)
                return null;

            UptimeServer.HostAvailability[] allData = await GetUptimeServer1dayCacheAsync();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData
                .Where(m => m?.Host?.Id != null)
                .Where(m => serverIds.Contains(m.Host.Id))
                .OrderByDescending(o => o?.Host?.Name)
                .ToList();
            return choosen;
        }

        public static async Task<UptimeServer.HostAvailability> AvailabilityForWeekByIdAsync(int serverId)
        {
            return (await AvailabilityForWeekByIdsAsync(new int[] { serverId })).FirstOrDefault();
        }

        public static async Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityForWeekByIdsAsync(IEnumerable<int> serverIds)
        {
            if (serverIds?.Count() == null)
                return null;
            if (serverIds.Count() == 0)
                return null;

            UptimeServer.HostAvailability[] allData = await GetUptimeServer7daysCacheAsync();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData
                .Where(m => serverIds.Contains(m.Host.Id))
                .OrderByDescending(o => o.Host.Name)
                .ToList();
            return choosen;
        }


        public static async Task<IEnumerable<UptimeServer.HostAvailability>> AvailabilityForDayByGroupAsync(string group)
        {
            int[] serverIds = HlidacStatu.Repositories.UptimeServerRepo.ServersIn(group).ToArray();
            return await AvailabilityForDayByIdsAsync(serverIds);
        }

        public static async Task<UptimeServer.HostAvailability> AvailabilityForDayById(int serverId)
        {
            return (await AvailabilityForDayByIdsAsync(new int[] { serverId })).FirstOrDefault();
        }

        public static async Task<List<UptimeServer.HostAvailability>> AllActiveServers24hoursStatAsync()
        {
            return (await AvailabilityForDayByIdsAsync(HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers()
                    .Select(m => m.Id)))
                .ToList();
        }

        public static async Task<List<UptimeServer.HostAvailability>> AllActiveServersWeekStatAsync()
        {
            return (await AvailabilityForWeekByIdsAsync(HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers()
                    .Select(m => m.Id)))
                .ToList();
        }
    }
}