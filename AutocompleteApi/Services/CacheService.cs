using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devmasters;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories;
using Serilog;
using Whisperer;

namespace HlidacStatu.AutocompleteApi.Services;

public class CacheService
{
    private readonly ILogger _logger;
    private const string AutocompleteFolder = "autocomplete";

    public Dictionary<string, StatusInfo> Status { get; init; } = new()
    {
        [nameof(Kindex)] = new StatusInfo(),
        [nameof(Company)] = new StatusInfo(),
        [nameof(UptimeServer)] = new StatusInfo(),
        [nameof(FullAutocomplete)] = new StatusInfo(),
    };

    // Index caches
    public CachedIndex<SubjectNameCache> Kindex { get; }
    public CachedIndex<Autocomplete> Company { get; }
    public CachedIndex<StatniWebyAutocomplete> UptimeServer { get; }
    public CachedIndex<Autocomplete> FullAutocomplete { get; }

    
    public CacheService(ILogger logger)
    {
        _logger = logger.ForContext<CacheService>();
        //cache setups, Cache Ctor should be fast, triggering data renew on background
        _logger.Information("Constructing Kindex cache");
        Kindex = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "kindex"),
            TimeSpan.FromDays(15),
            () => SubjectNameCache.GetCompanies().Values,
            new IndexingOptions<SubjectNameCache>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Ico}"
            },
            logger);
        Kindex.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Kindex));
        Kindex.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Kindex), b);

        _logger.Information("Constructing Company cache");
        Company = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "np_firmy"),
            TimeSpan.FromDays(15),
            () => AutocompleteRepo.GenerateAutocompleteFirmyOnly(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => $"{ts.Text}"
            },
            logger);
        Company.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Company));
        Company.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Company), b);

        _logger.Information("Constructing UptimeServer cache");
        UptimeServer = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "uptimeServer"),
            TimeSpan.FromHours(24),
            () => UptimeServerRepo.AllActiveServers().Select(uptimeServer => new StatniWebyAutocomplete(uptimeServer)),
            new IndexingOptions<StatniWebyAutocomplete>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Description} {ts.Ico} {ts.Url.Replace('.', ' ').Replace('/', ' ')}"
            },
            logger);
        UptimeServer.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(UptimeServer));
        UptimeServer.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(UptimeServer), b);
        
        _logger.Information("Constructing Full autocomplete cache");
        FullAutocomplete = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "full"),
            TimeSpan.FromHours(24),
            () => AutocompleteRepo.GenerateAutocomplete(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
                BoostSelector = bs => bs.PriorityMultiplier,
                FilterSelector = fs => fs.Type.ToLowerInvariant().RemoveAccents()
            },
            logger);
        FullAutocomplete.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(FullAutocomplete));
        FullAutocomplete.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(FullAutocomplete), b);
    }

    private void UpdateStatusStartTime(string name)
    {
        Status[name].StartTime = DateTime.Now;
    }
    
    private void UpdateStatusEndTime(string name, bool result)
    {
        var si = Status[name];
        si.EndTime = DateTime.Now;
        si.FinishedWithError = result;
    }
    
    public class StatusInfo
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool FinishedWithError { get; set; }
    }

}