using System;
using System.Collections.Generic;
using System.IO;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
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
        [nameof(Adresy)] = new StatusInfo(),
    };

    // Index caches
    public CachedIndex<SubjectNameCache> Kindex { get; }
    public CachedIndex<Autocomplete> Company { get; }
    public CachedIndex<StatniWebyAutocomplete> UptimeServer { get; }
    public CachedIndex<Autocomplete> FullAutocomplete { get; }
    public CachedIndex<AdresyKVolbam> Adresy { get; }

    
    public CacheService(ILogger logger)
    {
        _logger = logger.ForContext<CacheService>();
        //cache setups, Cache Ctor should be fast, triggering data renew on background
        string autocompleteFolder = Path.Combine(Init.WebAppDataPath, AutocompleteFolder);
        
        _logger.Information("Constructing Kindex cache");
        Kindex = CacheIndexFactory.CreateKindexCachedIndex(autocompleteFolder, _logger);
        Kindex.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Kindex));
        Kindex.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Kindex), b);

        _logger.Information("Constructing Company cache");
        Company = CacheIndexFactory.CreateCompanyCachedIndex(autocompleteFolder, _logger);
        Company.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Company));
        Company.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Company), b);

        _logger.Information("Constructing UptimeServer cache");
        UptimeServer = CacheIndexFactory.CreateUptimeServerCachedIndex(autocompleteFolder, _logger);
        UptimeServer.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(UptimeServer));
        UptimeServer.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(UptimeServer), b);
        
        _logger.Information("Constructing Full autocomplete cache");
        FullAutocomplete = CacheIndexFactory.CreateFullAutocompleteCachedIndex(autocompleteFolder, _logger);
        FullAutocomplete.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(FullAutocomplete));
        FullAutocomplete.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(FullAutocomplete), b);
        
        _logger.Information("Constructing Adresy cache");
        Adresy = CacheIndexFactory.CreateAdresyCachedIndex(autocompleteFolder, _logger);
        Adresy.CacheInitStarted += (_, _) => UpdateStatusStartTime(nameof(Adresy));
        Adresy.CacheInitFinished += (_, b) => UpdateStatusEndTime(nameof(Adresy), b);
    }

    private void UpdateStatusStartTime(string name)
    {
        Status[name].StartTime = DateTime.Now;
    }
    
    private void UpdateStatusEndTime(string name, string message)
    {
        var si = Status[name];
        si.EndTime = DateTime.Now;
        si.FinishedWithError = message;
    }
    
    public class StatusInfo
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string FinishedWithError { get; set; }
    }

}