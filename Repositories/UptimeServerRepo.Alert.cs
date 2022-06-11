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
            static Logger loggerAlert = Devmasters.Log.Logger.CreateLogger("UptimeServerAlert",
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


            public static AlertStatus CheckAndAlertServer(int serverId)
            {
                var status = CheckServer(serverId);
                var server = UptimeServerRepo.Load(serverId);

                AlertStatus? lastAlertStatus = (AlertStatus?)server.LastAlertedStatus;
                DateTime? lastAlertSent = server.LastAlertSent;
                UptimeServer.Availability.SimpleStatuses lastUptimeStatus = server.LastUptimeStatusToSimpleStatus();



                //no alert necessary
                switch (status)
                {
                    case AlertStatus.NoData:
                    case AlertStatus.ToSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (slow)", server.PublicUrl, status);
                        return status;
                    case AlertStatus.BackOkFromSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (backOkFromSlow)", server.PublicUrl, status);
                        return status;
                    default:
                        break;
                }


                //think more if to alert or not
                var twitter = new HlidacStatu.Lib.Data.External.Twitter(Lib.Data.External.Twitter.TwAccount.HlidacW);
                switch (status)
                {
                    case AlertStatus.NoChange:
                        if (lastAlertSent == null)
                        { //zadny alert nebyl ulozeny, uloz posledni status a pri selhani udelej Alert
                            UptimeServerRepo.SaveAlert(serverId, status);
                            if (lastUptimeStatus == UptimeServer.Availability.SimpleStatuses.Bad)
                            {
                                twitter.NewTweetAsync($"Server {server.Name} je nedostupný. Více podrobností na {server.pageUrl}.")
                                    .ConfigureAwait(false).GetAwaiter().GetResult();
                            }
                        }
                        else if ((DateTime.Now - lastAlertSent.Value).TotalHours < 24)
                        {
                            //zadna zmena za 24 hodin, nic nedelej
                            return status;
                        }
                        else
                        {
                            //status je stejny vice nez 24 hodin, Bad status znovu Alertuj
                            UptimeServerRepo.SaveAlert(serverId, status);
                            if (lastUptimeStatus == UptimeServer.Availability.SimpleStatuses.Bad)
                            {
                                twitter.NewTweetAsync($"Server {server.Name} je stále nedostupný. Více podrobností na {server.pageUrl}.")
                                        .ConfigureAwait(false).GetAwaiter().GetResult();
                            }
                        }

                        return status;
                    case AlertStatus.ToFail:
                        loggerAlert.Warning("{server} -> {changedStatus} (fail)", server.PublicUrl, status);
                        
                        
                        twitter.NewTweetAsync($"Server {server.Name} je nedostupný. Více podrobností na {server.pageUrl}.").ConfigureAwait(false).GetAwaiter().GetResult();
                        UptimeServerRepo.SaveAlert(serverId, status);
                        break;
                    case AlertStatus.BackOk:
                        loggerAlert.Warning("{server} -> {changedStatus} (backOk)", server.PublicUrl, status);

                        twitter.NewTweetAsync($"Server {server.Name} je opět dostupný. Více podrobností na {server.pageUrl}.").ConfigureAwait(false).GetAwaiter().GetResult();
                        UptimeServerRepo.SaveAlert(serverId, status);
                        break;
                    default:
                        break;
                }
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

                int numToAnalyze = 10;

                if (avail.Length < numToAnalyze )
                    return AlertStatus.NoChange;


                return CheckServerLogic(serverId, avail, numToAnalyze);

            }

            //should be private
            public static AlertStatus CheckServerLogic(int serverId, UptimeServer.Availability[] availabilities, int numToChange = 2)
            {
                UptimeServer.Availability[] lastChecks = availabilities
                    .OrderByDescending(o=>o.Time)
                    .Take(numToChange)
                    .ToArray();


                //same status for last 2/numToChange 
                var lastChecksHaveSameStatus = lastChecks.All(m => m.SimpleStatus() == lastChecks.First().SimpleStatus());

                if (lastChecksHaveSameStatus == false)
                    return AlertStatus.NoChange;

                UptimeServer.Availability[] preLastChecks = availabilities.Skip(numToChange).Take(numToChange).ToArray();

                var preLastChecksHaveSameStatus = preLastChecks.All(m => m.SimpleStatus() == lastChecks.First().SimpleStatus());

                if (preLastChecksHaveSameStatus)
                    return ChangeStatusLogic( 
                        preLastChecks.First().SimpleStatus(), 
                        lastChecks.First().SimpleStatus()
                        );

                //predchozí se liší
                //z predchoziho intervalu vezmu ten necastejsi stav co byl
                preLastChecks = availabilities.Skip(numToChange).Take(numToChange).ToArray();
                var mostDetectedStatus_all = preLastChecks
                    .GroupBy(k => k.SimpleStatus(), (k, v) => new { status = k, count = v.Count() })
                    .OrderByDescending(o => o.count).ThenByDescending(o => (int)o.status)
                    .ToArray();

                UptimeServer.Availability.SimpleStatuses? mostDetectedStatus = null;
                if (mostDetectedStatus_all.First().count == (numToChange * 2) / mostDetectedStatus_all.Count())
                {
                    //stejny pocet, rozhodni podle posledn�
                    var lastAlertStatus = (Alert.AlertStatus?)UptimeServerRepo.Load(serverId).LastAlertedStatus;
                    switch (lastAlertStatus)
                    {
                        case AlertStatus.NoData:
                        case AlertStatus.NoChange:
                            mostDetectedStatus = null;
                            break;
                        case AlertStatus.ToSlow:
                            mostDetectedStatus =  UptimeServer.Availability.SimpleStatuses.OK;
                            break;
                        case AlertStatus.ToFail:
                            mostDetectedStatus = UptimeServer.Availability.SimpleStatuses.Bad;
                            break;
                        case AlertStatus.BackOk:
                            mostDetectedStatus = UptimeServer.Availability.SimpleStatuses.OK;
                            break;
                        case AlertStatus.BackOkFromSlow:
                            mostDetectedStatus = UptimeServer.Availability.SimpleStatuses.OK;
                            break;
                        default:
                            break;
                    }
                }

                //pokud stale nevim, rozhodni podle priorit
                mostDetectedStatus = mostDetectedStatus  ?? mostDetectedStatus_all
                                        .Select(m=>m.status)
                                        .First();



                return ChangeStatusLogic(
                    mostDetectedStatus.Value,
                    lastChecks.First().SimpleStatus()
                    );
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