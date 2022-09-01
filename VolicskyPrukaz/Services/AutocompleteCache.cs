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
                var acfile = File.ReadAllBytes("adresy.acf");
                
                return Index<AdresyKVolbam>.Deserialize(acfile); //todo: for testing purposes, in prod it needs to be changed to line below 
                //return AdresyRepo.CreateAutocompleteForAdresyAsync().GetAwaiter().GetResult();
            }
        );

    public AutocompleteCache()
    {
        _ = Task.Run(() =>
        {
            return _cache.Get();
        });
    }

    public Index<AdresyKVolbam> GetIndex()
    {
        return _cache.Get();
    }

}