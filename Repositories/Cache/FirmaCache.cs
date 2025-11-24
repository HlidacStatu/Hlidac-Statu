using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    
    
    private static readonly IFusionCache Cache =
        Caching.CacheFactory.CreateDefaultCacheWithL2Support(nameof(FirmaCache), nameof(FirmaCache));

    

    public static async Task<string> GetProductAsync(int id, int delayInMs)
    {
        var product = await Cache.GetOrSetAsync<string>($"product:{id}",
            _ => GetProductFromAsync(id, delayInMs)
        );
        return product;
    }

    
    //factories
    private static async Task<string> GetProductFromAsync(int id, int delayInMs)
    {
        return $"produkt {id} je {productCouner++}";
    }

    
}