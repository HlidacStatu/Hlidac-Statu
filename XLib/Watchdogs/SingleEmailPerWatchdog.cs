using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib.Watchdogs
{
    public class SingleEmailPerWatchdog
    {
        public  static void SendWatchdogs(IEnumerable<WatchDog> watchdogs,
            bool force = false, string[] specificContacts = null,
            DateTime? fromSpecificDate = null, DateTime? toSpecificDate = null,
            string openingText = null,
            int? maxDegreeOfParallelism = 20,
            Action<string> logOutputFunc = null,
            Action<Devmasters.Batch.ActionProgressData> progressOutputFunc = null
            )
        {
            bool saveWatchdogStatus =
                force == false
                && fromSpecificDate.HasValue == false
                && toSpecificDate.HasValue == false;

            Util.Consts.Logger.Info($"SingleEmailPerWatchdog Start processing {watchdogs.Count()} watchdogs.");

            Devmasters.Batch.Manager.DoActionForAll<WatchDog>(watchdogs,
                (userWatchdog) =>
                {
                    ApplicationUser user = userWatchdog.UnconfirmedUser();

                    Util.Consts.Logger.Info("SingleEmailPerWatchdog watchdog sending {userWatchdogId} to {email}.",
                        userWatchdog.Id, user.Email);
                    Mail.SendStatus res = Mail.SendWatchdogAsync(userWatchdog, user,
                        force, specificContacts, fromSpecificDate, toSpecificDate, openingText)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                    Util.Consts.Logger.Info("SingleEmailPerWatchdog watchdog {userWatchdogId} to {email} sent result {result}.",
                        userWatchdog.Id, user.Email, res.ToString());

                    return new Devmasters.Batch.ActionOutputData();
                },
                logOutputFunc, progressOutputFunc,
                true, maxDegreeOfParallelism: maxDegreeOfParallelism, prefix: "SingleEmailPerWatchdog "
                , monitor: new MonitoredTaskRepo.ForBatch()
                ); 

            Util.Consts.Logger.Info($"SingleEmailPerWatchdog Done processing {watchdogs.Count()} watchdogs.");

        }
    }
}
