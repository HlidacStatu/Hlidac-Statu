using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devmasters;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories;
using Whisperer;

namespace HlidacStatu.AutocompleteApi.Services;

public class Caches
{
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

    
    public Caches()
    {
        //cache setups, Cache Ctor should be fast, triggering data renew on background
        Kindex = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "kindex"),
            TimeSpan.FromDays(15),
            () => SubjectNameCache.GetCompanies().Values,
            new IndexingOptions<SubjectNameCache>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Ico}"
            });
        Kindex.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Kindex));
        Kindex.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Kindex), b);

        Company = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "np_firmy"),
            TimeSpan.FromDays(15),
            () => AutocompleteRepo.GenerateAutocompleteFirmyOnly(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => $"{ts.Text}"
            });
        Company.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Company));
        Company.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Company), b);

        UptimeServer = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "uptimeServer"),
            TimeSpan.FromHours(24),
            () => UptimeServerRepo.AllActiveServers().Select(uptimeServer => new StatniWebyAutocomplete(uptimeServer)),
            new IndexingOptions<StatniWebyAutocomplete>()
            {
                TextSelector = ts => $"{ts.Name} {ts.Description} {ts.Ico} {ts.Url.Replace('.', ' ').Replace('/', ' ')}"
            });
        UptimeServer.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(UptimeServer));
        UptimeServer.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(UptimeServer), b);
        
        FullAutocomplete = new(
            Path.Combine(Init.WebAppDataPath, AutocompleteFolder, "full"),
            TimeSpan.FromHours(24),
            () => AutocompleteRepo.GenerateAutocomplete(),
            new IndexingOptions<Autocomplete>()
            {
                TextSelector = ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
                BoostSelector = bs => bs.PriorityMultiplier,
                FilterSelector = fs => fs.Type.ToLowerInvariant().RemoveAccents()
            });
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