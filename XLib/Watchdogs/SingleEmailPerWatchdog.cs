using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.XLib.Watchdogs
{
    public class SingleEmailPerWatchdog
    {
        public static void SendWatchdogs(IEnumerable<WatchDog> watchdogs,
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

                    var res = Mail.SendWatchdogAsync(userWatchdog, user,
                        force, specificContacts, fromSpecificDate, toSpecificDate, openingText);

                    Util.Consts.Logger.Info($"SingleEmailPerWatchdog watchdog {userWatchdog.Id} sent result {res.ToString()}.");

                    return new Devmasters.Batch.ActionOutputData();
                },
                logOutputFunc, progressOutputFunc,
                true, maxDegreeOfParallelism: maxDegreeOfParallelism, prefix: "SingleEmailPerWatchdog "
                );

            Util.Consts.Logger.Info($"SingleEmailPerWatchdog Done processing {watchdogs.Count()} watchdogs.");

        }
    }
}
