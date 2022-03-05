using Devmasters.Log;

using HlidacStatu.Entities;

using System;
using System.Data;
using System.Data.SqlClient;
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
                //do alert
                switch (status)
                {
                    case AlertStatus.NoData:
                    case AlertStatus.NoChange:
                        break;
                    case AlertStatus.ToSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (slow)", server.PublicUrl, status);
                        break;
                    case AlertStatus.ToFail:
                        loggerAlert.Warning("{server} -> {changedStatus} (fail)", server.PublicUrl, status);
                        UptimeServerRepo.SaveAlert(serverId, status);
                        break;
                    case AlertStatus.BackOk:
                        loggerAlert.Warning("{server} -> {changedStatus} (backOk)", server.PublicUrl, status);
                        UptimeServerRepo.SaveAlert(serverId, status);
                        break;
                    case AlertStatus.BackOkFromSlow:
                        loggerAlert.Info("{server} -> {changedStatus} (backOkFromSlow)", server.PublicUrl, status);
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

                int numToAnalyze = 2;

                if (avail.Length < numToAnalyze * 3)
                    return AlertStatus.NoChange;


                return CheckServerLogic(avail, numToAnalyze);

            }

            //should be private
            public static AlertStatus CheckServerLogic(UptimeServer.Availability[] availabilities, int numToChange = 2)
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

                //predchozí se lisí
                //z predchoziho 2x delsiho intervalu vezmu ten necastejsi stav co byl
                preLastChecks = availabilities.Skip(numToChange).Take(numToChange*2).ToArray();
                UptimeServer.Availability.SimpleStatuses mostDetectedStatus = preLastChecks
                    .GroupBy(k => k.SimpleStatus(), (k, v) => new { k = k, v = v.Count() } )
                    .OrderByDescending(o=>o.v).ThenByDescending(o=>(int)o.k)
                    .Select(m=>m.k)
                    .First();

                return ChangeStatusLogic(
                    mostDetectedStatus,
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