using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatyUredniku.Cache
{
    public static class OsobyRolesCache
    {
        public class osobaInfo
        {
            public string Jmeno { get; set; }

            public string Prijmeni { get; set; }
            public string FullName => $"{Jmeno} {Prijmeni}".Trim();
            public string Role { get; set; }
        }
        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<Dictionary<string, osobaInfo>> _cacheRoles =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<Dictionary<string, osobaInfo>>(
                TimeSpan.FromHours(12), "OsobyRolesCache",
                (obj) =>
                {
                    System.Collections.Concurrent.ConcurrentDictionary<string, osobaInfo> res = new System.Collections.Concurrent.ConcurrentDictionary<string, osobaInfo>();
                    var nameids = PpRepo.GetNameIdsForGroupAsync(PpRepo.PoliticianGroup.Vse)
                        .ConfigureAwait(false).GetAwaiter().GetResult()
                        .Distinct();

                    Devmasters.Batch.Manager.DoActionForAll(nameids,
                        (nameid) =>
                        {
                            var o = Osoby.GetByNameId.Get(nameid);
                            if (o != null)
                            {
                                res[nameid] = new osobaInfo()
                                {
                                    Jmeno = o.Jmeno,
                                    Prijmeni = o.Prijmeni,
                                    Role = o.MainRolesToString(PpRepo.DefaultYear)
                                };
                            }
                            return new Devmasters.Batch.ActionOutputData();
                        },
                        null, null,
                        true, 10,
                        prefix: "OsobyRolesCache ", monitor: new MonitoredTaskRepo.ForBatch()
                    );
                    return res.ToDictionary();
                });

        public static osobaInfo Get(string nameId)
        {
            if (_cacheRoles.Get().ContainsKey(nameId))
            {
                return _cacheRoles.Get()[nameId];
            }
            else
            {
                var o = Osoby.GetByNameId.Get(nameId);
                if (o != null)
                {
                    var allDict = _cacheRoles.Get();
                    allDict[nameId] = new osobaInfo()
                    {
                        Jmeno = o.Jmeno,
                        Prijmeni = o.Prijmeni,
                        Role = o.MainRolesToString(PpRepo.DefaultYear)
                    };
                    _= _cacheRoles.ForceRefreshCache(allDict);
                    return allDict[nameId];

                }
                else 
                    return new osobaInfo()
                    {
                        Jmeno = "",
                        Prijmeni = "",
                        Role = ""
                    };
            }
        }

    }
}
