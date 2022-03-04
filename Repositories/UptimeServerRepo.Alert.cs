using Devmasters.Log;

using HlidacStatu.Entities;

using System;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static partial class UptimeServerRepo
    {
        public static partial class Alert
        {
            static Logger logger = Devmasters.Log.Logger.CreateLogger("UptimeServerAlert",
            Devmasters.Log.Logger.DefaultConfiguration()
                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                );

            public enum AlertStatus
            {
                NoData = -1,
                NoChange = 0,
                ToSlow = 10,
                ToFail = 50,
                BackOk = 100,
            }


            public static AlertStatus CheckAndAlertServer(int serverId)
            {
                var status = CheckServer(serverId);
                return status;
            }

            public static AlertStatus CheckServer(int serverId)
            {
                var server = UptimeServerRepo.ShortAvailability(new[] { serverId }, TimeSpan.FromMinutes(30));

                UptimeServer.Availability[] avail = server.First().Data
                    .OrderByDescending(o => o.Time)
                    .ToArray();

                if (avail.Length == 0)
                    return AlertStatus.NoData;

                int numToAnalyzeBeforeLast = 2;

                if (avail.Length <= numToAnalyzeBeforeLast)
                    return AlertStatus.NoChange;

                var last = avail.First();
                if (last.Status() == UptimeSSL.Statuses.Pomalé)
                    numToAnalyzeBeforeLast=numToAnalyzeBeforeLast+2;

                var pre = avail.Skip(1).Take(numToAnalyzeBeforeLast);

                if (pre.All(m => m.Status() == last.Status()))
                    return AlertStatus.NoChange;

                var HasPreSameStatus = pre.All(m=>m.Status() == pre.First().Status());
                if (HasPreSameStatus)
                {
                    return ChangeStatusLogic(pre.First().Status(), last.Status());
                }

                return AlertStatus.NoChange;
            }

            private static AlertStatus ChangeStatusLogic(UptimeSSL.Statuses from, UptimeSSL.Statuses to)
            {

                switch (from)
                {
                    case UptimeSSL.Statuses.OK:
                        switch (to)
                        {
                            case UptimeSSL.Statuses.OK:
                                return AlertStatus.NoChange;
                            case UptimeSSL.Statuses.Pomalé:
                                return AlertStatus.ToSlow;
                            case UptimeSSL.Statuses.Nedostupné:
                            case UptimeSSL.Statuses.TimeOuted:
                            case UptimeSSL.Statuses.BadHttpCode:
                                return AlertStatus.ToFail;
                            case UptimeSSL.Statuses.Unknown:
                            default:
                                return AlertStatus.NoData;
                        }
                    case UptimeSSL.Statuses.Pomalé:
                        switch (to)
                        {
                            case UptimeSSL.Statuses.OK:
                                return AlertStatus.BackOk;
                            case UptimeSSL.Statuses.Pomalé:
                                return AlertStatus.NoChange;
                            case UptimeSSL.Statuses.Nedostupné:
                            case UptimeSSL.Statuses.TimeOuted:
                            case UptimeSSL.Statuses.BadHttpCode:
                                return AlertStatus.ToFail;
                            case UptimeSSL.Statuses.Unknown:
                            default:
                                return AlertStatus.NoData;
                        }
                    case UptimeSSL.Statuses.Nedostupné:
                    case UptimeSSL.Statuses.TimeOuted:
                    case UptimeSSL.Statuses.BadHttpCode:
                        switch (to)
                        {
                            case UptimeSSL.Statuses.OK:
                                return AlertStatus.BackOk;
                            case UptimeSSL.Statuses.Pomalé:
                                return AlertStatus.ToSlow;
                            case UptimeSSL.Statuses.Nedostupné:
                            case UptimeSSL.Statuses.TimeOuted:
                            case UptimeSSL.Statuses.BadHttpCode:
                                return AlertStatus.NoChange;
                            case UptimeSSL.Statuses.Unknown:
                            default:
                                return AlertStatus.NoData;
                        }
                    case UptimeSSL.Statuses.Unknown:
                    default:
                        return AlertStatus.NoData;
                }
            }

        }

    }
}