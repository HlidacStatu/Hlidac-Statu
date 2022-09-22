using System;
using System.IO;
using System.Linq;
using Devmasters;
using HlidacStatu.Entities;
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
                TextSelector = ts => $"{ts.Text}"
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
                TextSelector = ts => $"{ts.Name} {ts.Description} {ts.Ico} {ts.Url.Replace('.', ' ').Replace('/', ' ')}"
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
                FilterSelector = fs => fs.Type.ToLowerInvariant().RemoveAccents()
            },
            logger);
    }
}