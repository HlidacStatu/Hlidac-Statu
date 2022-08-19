using FullTextSearch;
using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories;

namespace VolebniPrukaz.Services;

public class AutocompleteCache
{
    private const string CacheKey = "adresy";
    private static TimeSpan _expiration = TimeSpan.FromDays(7);
    
    private Devmasters.Cache.LocalMemory.AutoUpdatedCache<Index<AdresyKVolbam>> _cache =
        new(_expiration, CacheKey, _ =>
            {
                return AdresyRepo.CreateAutocompleteForAdresyAsync().GetAwaiter().GetResult();
            }
        );

    public Index<AdresyKVolbam> GetIndex()
    {
        return _cache.Get();
    }
    
}