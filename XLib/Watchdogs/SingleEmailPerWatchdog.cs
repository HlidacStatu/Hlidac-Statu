using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.XLib.Watchdogs
{
    public class SingleEmailPerWatchdog
    {
        private static readonly ILogger _logger = Log.ForContext<SingleEmailPerWatchdog>();
        
        public async static Task SendWatchdogsAsync(IEnumerable<WatchDog> watchdogs,
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

            _logger.Information($"SingleEmailPerWatchdog Start processing {watchdogs.Count()} watchdogs.");

            await Devmasters.Batch.Manager.DoActionForAllAsync<WatchDog>(watchdogs,
               async (userWatchdog) =>
                {
                    ApplicationUser user = userWatchdog.UnconfirmedUser();

                    _logger.Information("SingleEmailPerWatchdog watchdog sending {userWatchdogId} to {email}.",
                        userWatchdog.Id, user.Email);
                    Mail.SendStatus res = await Mail.SendWatchdogAsync(userWatchdog, user,
                        force, specificContacts, fromSpecificDate, toSpecificDate, openingText);

                    _logger.Information("SingleEmailPerWatchdog watchdog {userWatchdogId} to {email} sent result {result}.",
                        userWatchdog.Id, user.Email, res.ToString());

                    return new Devmasters.Batch.ActionOutputData();
                },
                logOutputFunc, progressOutputFunc,
                true, maxDegreeOfParallelism: maxDegreeOfParallelism, prefix: "SingleEmailPerWatchdog "
                , monitor: new MonitoredTaskRepo.ForBatch()
                ); 

            _logger.Information($"SingleEmailPerWatchdog Done processing {watchdogs.Count()} watchdogs.");

        }
    }
}
