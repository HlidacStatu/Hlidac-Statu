using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.XLib.Watchdogs
{
    public class SingleEmailPerUser
    {
        private static readonly ILogger _logger = Log.ForContext<SingleEmailPerUser>();

        public async static Task SendWatchdogsAsync(IEnumerable<WatchDog> watchdogs,
            bool force = false, string[] specificContacts = null,
            DateTime? fromSpecificDate = null, DateTime? toSpecificDate = null,
            string openingText = null,
            int? maxDegreeOfParallelism = 20,
            Action<string> logOutputFunc = null,
            Devmasters.Batch.IProgressWriter  progressOutputFunc = null
            )
        {
            bool saveWatchdogStatus =
                force == false
                && fromSpecificDate.HasValue == false
                && toSpecificDate.HasValue == false;

            _logger.Information($"SingleEmailPerUser Start processing {watchdogs.Count()} watchdogs.");


            Dictionary<string, WatchDog[]> groupedByUserNoSpecContact = watchdogs
                .Where(w => w != null)
                .Where(w => w.UnconfirmedUser() != null)
                .Where(m => string.IsNullOrEmpty(m.SpecificContact))
                .GroupBy(k => k.UnconfirmedUser().Id,
                        v => v,
                        (k, v) => new { key = k, val = v.ToArray() }
                        )
                .ToDictionary(k => k.key, v => v.val);

            _logger.Information($"SingleEmailPerUser {groupedByUserNoSpecContact.Count()} emails.");


            await Devmasters.Batch.Manager.DoActionForAllAsync<KeyValuePair<string, WatchDog[]>>(groupedByUserNoSpecContact,
                async (kv) =>
                {
                    WatchDog[] userWatchdogs = kv.Value;

                    ApplicationUser user = null;
                    using (DbEntities db = new DbEntities())
                    {
                        user = db.Users.AsQueryable()
                        .Where(m => m.Id == kv.Key)
                        .FirstOrDefault();
                    }

                    _logger.Information("SingleEmailPerUser watchdog {userWatchdogId} sending to {email}.",
                        string.Join(",", userWatchdogs.Select(m => m.Id.ToString())), user.Email);
                    Mail.SendStatus res= Mail.SendStatus.SendingError;
                    try
                    {

                        res = await Mail.SendWatchdogsInOneEmailAsync(userWatchdogs, user,
                        force, specificContacts, fromSpecificDate, toSpecificDate, openingText);
                    }
                    catch (Exception e)
                    {
                        res = Mail.SendStatus.SendingError;
                        _logger.Error(e,
                            "SingleEmailPerUser watchdog {userWatchdogId} to {email} sent results with {exception}.", string.Join(",", userWatchdogs.Select(m => m.Id.ToString())), user.Email,res.ToString());
                    }

                    _logger.Information("SingleEmailPerUser watchdog {userWatchdogId} to {email} sent result {result}.",
                        string.Join(",",userWatchdogs.Select(m=>m.Id.ToString())), user.Email, res.ToString());
                    //_logger.Information($"SingleEmailPerUser {kv.Key} sent result {res.ToString()}.");

                    return new Devmasters.Batch.ActionOutputData();
                },
                logOutputFunc, progressOutputFunc,
                true, maxDegreeOfParallelism: maxDegreeOfParallelism, prefix:"Send watchdogs "
               , monitor: new MonitoredTaskRepo.ForBatch()
                );

            _logger.Information($"SingleEmailPerUser Done processing {watchdogs.Count()} watchdogs.");

        }
    }
}
