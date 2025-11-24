using System.Threading.Tasks;
using Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    private static readonly IFusionCache Cache =
        Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));


    public static async Task<InfoFact[]> GetInfoFactsAsync(Firma firma)
    {
        var infoFacts = await Cache.GetOrSetAsync($"_InfoFacts:{firma.ICO}",
            _ => FirmaExtension.GetDirectInfoFactsAsync(firma)
        );
        return infoFacts;
    }
    
    public static async Task InvalidateInfoFactsAsync(Firma firma)
    {
        await Cache.ExpireAsync($"_InfoFacts:{firma.ICO}");
    }

    public static async Task<Riziko[]> GetRizikoAsync(Firma f, int rok)
    {
        var rizika = await Cache.GetOrSetAsync($"_Rizika:{f.ICO}-{rok}",
            _ => FirmaExtension.GetDirectRizikoAsync(f, rok)
        );
        return rizika;
    }
    
    public static async Task InvalidateRizikoAsync(Firma f, int rok)
    {
        await Cache.ExpireAsync($"_Rizika:{f.ICO}-{rok}");
    }
}