using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

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
            public string Strana { get; set; }
        }

        private static IFusionCache _postgreSqlCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(OsobyRolesCache));

        private static async Task<Dictionary<string, osobaInfo>> GetOrSetCachedRolesAsync(int rok,
            Dictionary<string, osobaInfo>? data = null)
        {
            string cacheKey = $"OsobyRolesCache:{rok}";
            if (data is null)
            {
                return await _postgreSqlCache.GetOrSetAsync(cacheKey, async _ =>
                    {
                        System.Collections.Concurrent.ConcurrentDictionary<string, osobaInfo> res = new();
                        var nameids = (await PpRepo.GetNameIdsForGroupAsync(PpRepo.PoliticianGroup.Vse)).Distinct();

                        await Devmasters.Batch.Manager.DoActionForAllAsync(nameids, async (nameid) =>
                            {
                                var o = await OsobaCache.GetPersonByNameIdAsync(nameid);
                                if (o != null)
                                {
                                    res[nameid] = new osobaInfo()
                                    {
                                        Jmeno = o.Jmeno,
                                        Prijmeni = o.Prijmeni,
                                        Role = o.MainRolesToString(rok),
                                        Strana = await o.CurrentPoliticalPartyAsync()
                                    };
                                }

                                return new Devmasters.Batch.ActionOutputData();
                            },
                            null, null,
                            true, 10,
                            prefix: "OsobyRolesCache ", monitor: new MonitoredTaskRepo.ForBatch()
                        );
                        return res.ToDictionary();
                    },
                    options =>
                    {
                        options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12));
                        options.ModifyEntryOptionsFactoryTimeouts(factoryHardTimeout: TimeSpan.FromMinutes(10));
                    });
            }

            await _postgreSqlCache.SetAsync(cacheKey, data, options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12));
                options.ModifyEntryOptionsFactoryTimeouts(factoryHardTimeout: TimeSpan.FromMinutes(10));
            });

            return data;
        }

        public static async Task<osobaInfo> GetAsync(string nameId, int rok = PpRepo.DefaultYear)
        {
            var cachedRoles = await GetOrSetCachedRolesAsync(rok);

            if (cachedRoles.TryGetValue(nameId, out var role))
            {
                return role;
            }
            else
            {
                var o = await OsobaCache.GetPersonByNameIdAsync(nameId);
                if (o != null)
                {
                    var allDict = cachedRoles;
                    allDict[nameId] = new osobaInfo()
                    {
                        Jmeno = o.Jmeno,
                        Prijmeni = o.Prijmeni,
                        Role = o.MainRolesToString(PpRepo.DefaultYear)
                    };
                    await GetOrSetCachedRolesAsync(rok, allDict);
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