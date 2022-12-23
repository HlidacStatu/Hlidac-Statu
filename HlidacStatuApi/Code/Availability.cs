using Devmasters.DT;
using HlidacStatu.Entities;

namespace HlidacStatuApi.Code
{
    public class Availability
    {

        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer.HostAvailability[]> uptimeServersCache1Day =
      new Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(3),
          (obj) =>
          {
              Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
              sw.Start();
              var res = _availability(24);
              sw.Stop();
              HlidacStatuApi.Code.Log.Logger.Info("{action} updated of {part} in {duration} ms.", "updated", "uptimeServersCache1Day", sw.ElapsedMilliseconds);
              return res.ToArray();
          });

        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer.HostAvailability[]> uptimeServersCache7Days
       = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeServer.HostAvailability[]>(TimeSpan.FromMinutes(30),
           "uptimeServersCache7Days",
          (id) =>
          {
              Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
              sw.Start();

            #if DEBUG
              var res = uptimeServersCache1Day.Get();
            #else
              var res = _availability(7 * 24);
            #endif
              sw.Stop();
              HlidacStatuApi.Code.Log.Logger.Info("{action} updated of {part} in {duration} ms.", "updated", "uptimeServersCache7Days", sw.ElapsedMilliseconds);
              return res.ToArray();

          }
          );



        private static IEnumerable<UptimeServer.HostAvailability> _availability(int hoursBack)
        {
            int[] serverIds = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers().Select(m => m.Id).ToArray();
            return _availability(serverIds, hoursBack);
        }

        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, int hoursBack)
        {
            return _availability(serverIds, TimeSpan.FromHours(hoursBack));
        }

        private static IEnumerable<UptimeServer.HostAvailability> _availability(int[] serverIds, TimeSpan intervalBack)
        {
            UptimeServer[] allServers = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers();


            IEnumerable<HlidacStatu.Lib.Data.External.InfluxDb.ResponseItem> items = 
                new HlidacStatu.Lib.Data.External.InfluxDb.ResponseItem[] { };
            try
            {
                items = HlidacStatu.Lib.Data.External.InfluxDb.GetAvailbility(serverIds, intervalBack);
            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("Cannot read data from InfluxDb", e);
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
        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForDayByIds(IEnumerable<int> serverIds)
        {
            if (serverIds?.Count() == null)
                return null;
            if (serverIds.Count() == 0)
                return null;

            UptimeServer.HostAvailability[] allData = uptimeServersCache1Day.Get();

            List<UptimeServer.HostAvailability> choosen = new List<UptimeServer.HostAvailability>();
            choosen = allData
                .Where(m=>m?.Host?.Id != null)
                .Where(m => serverIds.Contains(m.Host.Id))
                .OrderByDescending(o => o?.Host?.Name)
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


        public static IEnumerable<UptimeServer.HostAvailability> AvailabilityForDayByGroup(string group)
        {
            int[] serverIds = HlidacStatu.Repositories.UptimeServerRepo.ServersIn(group).ToArray();
            return AvailabilityForDayByIds(serverIds);
        }
        public static UptimeServer.HostAvailability AvailabilityForDayById(int serverId)
        {
            return AvailabilityForDayByIds(new int[] { serverId }).FirstOrDefault();

        }

        public static List<UptimeServer.HostAvailability> AllActiveServers24hoursStat()
        {
            return AvailabilityForDayByIds(HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers().Select(m => m.Id))
                                    .ToList();
        }

        public static List<UptimeServer.HostAvailability> AllActiveServersWeekStat()
        {
            return AvailabilityForWeekByIds(HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers().Select(m => m.Id))
                                    .ToList();
        }

    }
}
