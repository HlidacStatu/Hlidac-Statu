using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devmasters;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories;
using Serilog;
using Whisperer;

namespace HlidacStatu.AutocompleteApi.Services;

public static class CacheIndexFactory
{
    public static CachedIndex<SubjectNameCache> CreateKindexCachedIndex(string path, ILogger logger)
    {
        return new(
            Path.Combine(path, "kindex"),
            TimeSpan.FromDays(15),
            () => SubjectNameCache.GetCompanies().Values,
            new IndexingOptions<SubjectNameCache>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Ico}"
            },
            logger);
    }
    
    public static CachedIndex<Autocomplete> CreateCompanyCachedIndex(string path, ILogger logger)
    {
        return new(
            Path.Combine(path, "np_firmy"),
            TimeSpan.FromDays(15),
            () => AutocompleteRepo.GenerateAutocompleteFirmyOnly(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => ts.Text
            },
            logger);
    }
    
    public static CachedIndex<StatniWebyAutocomplete> CreateUptimeServerCachedIndex(string path, ILogger logger)
    {
        return new(
            Path.Combine(path, "uptimeServer"),
            TimeSpan.FromHours(24),
            () => UptimeServerRepo.AllActiveServers().Select(uptimeServer => new StatniWebyAutocomplete(uptimeServer)),
            new IndexingOptions<StatniWebyAutocomplete>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Description} {ts.Ico} {ts.Url.Replace('/', ' ')}"
            },
            logger);
    }
    
    public static CachedIndex<Autocomplete> CreateFullAutocompleteCachedIndex(string path, ILogger logger)
    {
        return new(
            Path.Combine(path, "full"),
            TimeSpan.FromHours(24),
            () => AutocompleteRepo.GenerateAutocomplete(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
                BoostSelector = bs => bs.PriorityMultiplier,
                FilterSelector = fs => fs.Category.ToString("G")
            },
            logger);
    }
    
    public static CachedIndex<AdresyKVolbam> CreateAdresyCachedIndex(string path, ILogger logger)
    {
        return new(
            Path.Combine(path, "adresy"),
            TimeSpan.FromHours(24),
            () => AdresyRepo.GetAdresyKVolbamAsync().GetAwaiter().GetResult(),
            new IndexingOptions<AdresyKVolbam>()
            {
                TextSelector = ts => ts.Adresa,
                BoostSelector = bs =>
                {
                    // Velká města mají větší prioritu
                    if (bs.Obec.Contains("praha", StringComparison.InvariantCultureIgnoreCase))
                        return 2.6f;
                    if (bs.Obec.Contains("brno", StringComparison.InvariantCultureIgnoreCase))
                        return 2.5f;
                    if (bs.Obec.Contains("plzeň", StringComparison.InvariantCultureIgnoreCase))
                        return 2.4f;
                    if (bs.Obec.Contains("ostrava", StringComparison.InvariantCultureIgnoreCase))
                        return 2.3f;

                    // Typ Ovm má hodnoty = 5, 6, 7, 8 => výsledné číslo je hodnota mezi 1.2 - 1.8
                    return ((bs.TypOvm - 4) / 5f) + 1f;
                }
            },
            logger);
    }
}