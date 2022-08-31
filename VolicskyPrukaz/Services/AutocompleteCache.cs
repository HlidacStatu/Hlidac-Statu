using FullTextSearch;
using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories;

namespace VolicskyPrukaz.Services;

public class AutocompleteCache
{
    private const string CacheKey = "adresy";
    private static TimeSpan _expiration = TimeSpan.FromDays(7);
    
    private Devmasters.Cache.LocalMemory.AutoUpdatedCache<Index<AdresyKVolbam>> _cache =
        new(_expiration, CacheKey, _ =>
            {
                Console.WriteLine("Beru cache");
                return AdresyRepo.CreateAutocompleteForAdresyAsync().GetAwaiter().GetResult();
            }
        );

    public AutocompleteCache()
    {
        _ = Task.Run(() =>
        {
            Console.WriteLine("Začínám generovat cache");
            return _cache.Get();
        });
    }

    public Index<AdresyKVolbam> GetIndex()
    {
        return _cache.Get();
    }
    
}