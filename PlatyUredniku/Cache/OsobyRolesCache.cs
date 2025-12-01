using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlatyUredniku.Cache
{
    public static class OsobyRolesCache
    {
        static OsobyRolesCache()
        {
            // Initialize the cache manager
            for (int rok = PpRepo.DefaultYear; rok >= PpRepo.MinYear; rok--)
            {
                _ = _cacheRolesManager.Get(rok);
            }
        }
        public class osobaInfo
        {
            public string Jmeno { get; set; }

            public string Prijmeni { get; set; }
            public string FullName => $"{Jmeno} {Prijmeni}".Trim();
            public string Role { get; set; }
            public string Strana { get; set; }
        }


        private static Devmasters.Cache.LocalMemory.AutoUpdateCacheManager<Dictionary<string, osobaInfo>, int> _cacheRolesManager =
            new Devmasters.Cache.LocalMemory.AutoUpdateCacheManager<Dictionary<string, osobaInfo>, int>(
                "OsobyRolesCache",
                (rok) =>
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
                                    Role = o.MainRolesToString(rok),
                                    Strana = o.CurrentPoliticalParty()
                                };
                            }
                            return new Devmasters.Batch.ActionOutputData();
                        },
                        null, null,
                        true, 10,
                        prefix: "OsobyRolesCache ", monitor: new MonitoredTaskRepo.ForBatch()
                    );
                    return res.ToDictionary();
                }, TimeSpan.FromHours(12), rok => rok.ToString());

        public static osobaInfo Get(string nameId, int rok = PpRepo.DefaultYear)
        {
            if (_cacheRolesManager.Get(rok).ContainsKey(nameId))
            {
                return _cacheRolesManager.Get(rok)[nameId];
            }
            else
            {
                var o = Osoby.GetByNameId.Get(nameId);
                if (o != null)
                {
                    var allDict = _cacheRolesManager.Get(rok);
                    allDict[nameId] = new osobaInfo()
                    {
                        Jmeno = o.Jmeno,
                        Prijmeni = o.Prijmeni,
                        Role = o.MainRolesToString(PpRepo.DefaultYear)
                    };
                    _cacheRolesManager.Set(rok,allDict);
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
