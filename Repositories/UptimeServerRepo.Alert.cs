using System;
using System.Linq;

using Devmasters.Log;

using HlidacStatu.Entities;

namespace HlidacStatu.Repositories
{
    public static partial class UptimeServerRepo
    {
        public static partial class Alert
        {
            static Logger loggerAlert = Devmasters.Log.Logger.CreateLogger("HlidacStatu.UptimeServerAlert",
                Devmasters.Log.Logger.EmptyConfiguration()
                .AddLogStash(new Uri("http://10.10.150.203:5000"))
                //.WriteTo.Http("http://10.10.150.203:5000",
                //    batchPostingLimit: 2,
                //    queueLimit: 2,
                //    textFormatter: new Elastic.CommonSchema.Serilog.EcsTextFormatter(new Elastic.CommonSchema.Serilog.EcsTextFormatterConfiguration()),
                //    batchFormatter: new Serilog.Sinks.Http.BatchFormatters.ArrayBatchFormatter()
                //    )
                //.WriteTo.Console()
                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                );

            static Alert()
            {
                loggerAlert.Info("Starting logger for UptimeServerAlert");
            }
            public enum AlertStatus
            {
                NoData = -1,
                NoChange = 0,
                ToSlow = 10,
                ToFail = 50,
                BackOk = 100,
                BackOkFromSlow = 101,
            }

            public class CheckAndAlertResult
            {
                public AlertStatus AlertStatus { get; set; }
                public TimeSpan LengthOfFail { get; set; } = TimeSpan.Zero;
            }

            public static CheckAndAlertResult CheckAndAlertServer(int serverId, DateTime? fromD = null, DateTime? toD = null)
            {
                CheckAndAlertResult alertStatus = CheckAlertStatusForServerFromInflux(serverId, fromD, toD);

                var serverLastSavedStatusInDb = UptimeServerRepo.Load(serverId);
                AlertStatus? lastAlertStatus = (AlertStatus?)serverLastSavedStatusInDb.LastAlertedStatus;
                DateTime lastAlertSent = serverLastSavedStatusInDb.LastAlertSent ?? DateTime.MinValue;
                UptimeServer.Availability.SimpleStatuses lastUptimeStatusInDb = serverLastSavedStatusInDb.LastUptimeStatusToSimpleStatus();


                //no alert necessary
                switch (alertStatus.AlertStatus)
                {
                    case AlertStatus.NoData:
                    case AlertStatus.ToSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (slow)", serverLastSavedStatusInDb.PublicUrl, alertStatus);
                        return alertStatus;
                    case AlertStatus.BackOkFromSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (backOkFromSlow)", serverLastSavedStatusInDb.PublicUrl, alertStatus);
                        return alertStatus;
                    default:
                        break;
                }

                string lengthOfFailInMin = Devmasters.Lang.CS.Plural.Get((int)alertStatus.LengthOfFail.TotalMinutes, HlidacStatu.Util.Consts.czCulture,
                    "jednu minutu", "{0} minuty", "{0} minut"
                    );

                //think more if to alert or not
                var twitter = new HlidacStatu.Lib.Data.External.Twitter(Lib.Data.External.Twitter.TwAccount.HlidacW);
                switch (alertStatus.AlertStatus)
                {
                    case AlertStatus.NoChange:
                        if (lastAlertSent == null)
                        {
                            //zadny alert nebyl ulozeny, uloz posledni status a pri selhani udelej Alert
                            //UptimeServerRepo.SaveAlert(serverId, alertStatus.AlertStatus);
                            //if ( alertStatus. == AlertStatus.)
                            //{
                            //    twitter.NewTweetAsync($"Server {serverLastSavedStatusInDb.Name} je nedostupný. Více podrobností na {serverLastSavedStatusInDb.pageUrl}.")
                            //        .ConfigureAwait(false).GetAwaiter().GetResult();
                            //}
                        }
                        else if ((DateTime.Now - lastAlertSent).TotalHours < 24)
                        {
                            //zadna zmena za 24 hodin, nic nedelej
                            return alertStatus;
                        }
                        else
                        {
                            //status je stejny vice nez 24 hodin, Bad status znovu Alertuj
                            UptimeServerRepo.SaveAlert(serverId, alertStatus.AlertStatus);
                            if (lastUptimeStatusInDb == UptimeServer.Availability.SimpleStatuses.Bad)
                            {
                                _= twitter.NewTweetAsync($"Server {serverLastSavedStatusInDb.Name} je stále nedostupný. Více podrobností na {serverLastSavedStatusInDb.pageUrl}.")
                                        .ConfigureAwait(false).GetAwaiter().GetResult();
                            }
                        }

                        return alertStatus;
                    case AlertStatus.ToFail:
                        loggerAlert.Warning("{server} -> {changedStatus} (fail)", serverLastSavedStatusInDb.PublicUrl, alertStatus);
                        if (
                            alertStatus.LengthOfFail.TotalMinutes > 4 
                            && (DateTime.Now - lastAlertSent).TotalHours > 2
                            )
                        {
                            UptimeServerRepo.SaveAlert(serverId, alertStatus.AlertStatus);
                            _ = twitter.NewTweetAsync($"Server {serverLastSavedStatusInDb.Name} je nedostupný asi {lengthOfFailInMin}. Více podrobností na {serverLastSavedStatusInDb.pageUrl}.")
                                .ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        break;
                    case AlertStatus.BackOk:

                        if (lastAlertStatus.HasValue && lastAlertStatus.Value == AlertStatus.BackOk )
                        {
                            // do nothing
                        }
                        else if (
                            alertStatus.LengthOfFail.TotalMinutes > 4
                            && (DateTime.Now - lastAlertSent).TotalHours > 2
                            )
                        {
                            loggerAlert.Warning("{server} -> {changedStatus} (backOk)", serverLastSavedStatusInDb.PublicUrl, alertStatus);
                            UptimeServerRepo.SaveAlert(serverId, alertStatus.AlertStatus);
                            _ = twitter.NewTweetAsync($"Server {serverLastSavedStatusInDb.Name} byl nedostupný asi {lengthOfFailInMin}, nyní je opět dostupný. Více podrobností na {serverLastSavedStatusInDb.pageUrl}.")
                                .ConfigureAwait(false).GetAwaiter().GetResult();
                        }
                        break;
                    default:
                        break;
                }
                return alertStatus;
            }


            public static CheckAndAlertResult CheckAlertStatusForServerFromInflux(int serverId, DateTime? fromD = null, DateTime? toD = null)
            {
                var timeBack = TimeSpan.FromMinutes(30);
                if (fromD.HasValue)
                {
                    timeBack = (DateTime.Now - fromD.Value).Add(TimeSpan.FromMinutes(10));
                }

                var serverAvail = UptimeServerRepo.DirectShortAvailability(new[] { serverId }, timeBack)
                    .FirstOrDefault();
                if (fromD.HasValue)
                {
                    toD = toD ?? DateTime.Now;

                    serverAvail = new UptimeServer.HostAvailability(serverAvail.Host,
                        serverAvail.Data.Where(m => m.Time >= fromD.Value && m.Time <= toD.Value)
                        );

                }

                UptimeServer.Availability[] avail = serverAvail.Data
                    .OrderByDescending(o => o.Time)
                    .ToArray();

                if (avail.Length == 0)
                    return new CheckAndAlertResult() { AlertStatus = AlertStatus.NoData };

                int numToAnalyze = 10;

                if (avail.Length < numToAnalyze)
                    return new CheckAndAlertResult() { AlertStatus = AlertStatus.NoChange };


                CheckAndAlertResult ret = CheckServerLogic(serverId, avail, numToAnalyze);
                return ret;
            }

            //should be private
            public static CheckAndAlertResult CheckServerLogic(int serverId, UptimeServer.Availability[] availabilities, int numToAnalyze = 2)
            {
                UptimeServer.Availability[] lastChecks = availabilities
                    .OrderByDescending(o => o.Time)
                    .Take(numToAnalyze)
                    .ToArray();

                var lengthOfFail = TimeSpan.FromMinutes( lastChecks.Count(m => m.SimpleStatus() == UptimeServer.Availability.SimpleStatuses.Bad));

                var lastChecks_Majority = lastChecks
                    .GroupBy(k => k.SimpleStatus(), (k, v) => new { status = k, percentage = ((decimal)v.Count()/(decimal)lastChecks.Count()) })
                    .OrderByDescending(o => o.percentage)
                    .ToArray();


                //zadny status neni majoritni
                if (lastChecks_Majority.Count(m=>m.percentage>=0.75m) == 0)
                    return new CheckAndAlertResult() { AlertStatus = AlertStatus.NoChange };

                var lastChecks_MajorityStatus = lastChecks_Majority.First().status;

                UptimeServer.Availability[] preLastChecks = availabilities.Skip(numToAnalyze).Take(numToAnalyze).ToArray();

                var prelastChecks_Majority = preLastChecks
                    .GroupBy(k => k.SimpleStatus(), (k, v) => new { status = k, percentage = ((decimal)v.Count() / (decimal)lastChecks.Count()) })
                    .OrderByDescending(o => o.percentage)
                    .ToArray();

                if (prelastChecks_Majority.Count(m => m.percentage >= 0.5m) == 0)
                    return new CheckAndAlertResult() { AlertStatus = AlertStatus.NoChange};

                var prelastChecks_MajorityStatus = prelastChecks_Majority.First().status;


                var alertStatus= ChangeStatusLogic(
                    prelastChecks_MajorityStatus,
                    lastChecks_MajorityStatus
                    );

                return new CheckAndAlertResult() { AlertStatus = alertStatus, LengthOfFail = lengthOfFail };
            }
            private static AlertStatus ChangeStatusLogic(UptimeServer.Availability.SimpleStatuses from, UptimeServer.Availability.SimpleStatuses to)
            {

                switch (from)
                {
                    case UptimeServer.Availability.SimpleStatuses.OK:
                        switch (to)
                        {
                            case UptimeServer.Availability.SimpleStatuses.OK:
                                return AlertStatus.NoChange;
                            case UptimeServer.Availability.SimpleStatuses.Bad:
                                return AlertStatus.ToFail;
                            case UptimeServer.Availability.SimpleStatuses.Unknown:
                            default:
                                return AlertStatus.NoData;
                        }
                    case UptimeServer.Availability.SimpleStatuses.Bad:
                        switch (to)
                        {
                            case UptimeServer.Availability.SimpleStatuses.OK:
                                return AlertStatus.BackOk;
                            case UptimeServer.Availability.SimpleStatuses.Bad:
                                return AlertStatus.NoChange;
                            case UptimeServer.Availability.SimpleStatuses.Unknown:
                            default:
                                return AlertStatus.NoData;
                        }
                    case UptimeServer.Availability.SimpleStatuses.Unknown:
                    default:
                        return AlertStatus.NoData;
                }
            }

        }

    }
}