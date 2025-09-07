using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Cache;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories;

public static partial class PpRepo
{
    public static class Cached
    {
        public static async Task<List<PpPrijem>> GetPrijmyPolitikaAsync(string id, int rok)
        {
            var detail = await CacheService.Cache.GetOrSetAsync<List<PpPrijem>>(
                $"{nameof(PpRepo.GetPrijmyPolitikaAsync)}_{id}_{rok}-politici",
                _ => PpRepo.GetPrijmyPolitikaAsync(id, rok)
            );
            return detail;
        }
        
        public static List<string> GetPolitickeStranyForFilter()
        {
            return CacheService.Cache.GetOrSet<List<string>>(
                "politickeStranyFilterData",
                _ =>
                {
                    List<string> politickeStranyIca =
                    [
                        "71443339",
                        "00442704",
                        "00496936",
                        "16192656",
                        "71339698",
                        "10742409",
                        "17085438",
                        "00409171",
                        "04134940",
                        "26673908",
                        "00409740",
                        "71339728",
                        "08288909"
                    ];

                    return politickeStranyIca.Select(ZkratkaStranyRepo.NazevStranyForIco).ToList();
                }
            );
        }
    }
}