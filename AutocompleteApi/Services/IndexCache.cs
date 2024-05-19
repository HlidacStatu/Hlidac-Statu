using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.AutocompleteApi.Autocompletes;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.LibCore.Enums;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using MinimalEntities;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;

namespace HlidacStatu.AutocompleteApi.Services;

public class IndexCache
{

    private readonly ILogger logger = Log.ForContext<IndexCache>();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Dictionary<AutocompleteIndexType, IIndexStrategy?> _indexStrategies;
    private ConcurrentDictionary<AutocompleteIndexType, DateTime?> LastUpdateRun { get; set; } = new();

    public IndexCache(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;

        _indexStrategies = new();
        foreach (var autocompleteIndexType in Enum.GetValues<AutocompleteIndexType>())
        {
            _indexStrategies.TryAdd(autocompleteIndexType, null);
        }
    }

    public Status? Status()
    {
        if (!LastUpdateRun.Any())
            return null;

        return new Status()
        {
            LastUpdates = LastUpdateRun.ToDictionary(ks => ks.Key, vs => vs.Value),
            KindexDataStamp = LatestIndexStamp(AutocompleteIndexType.KIndex),
            KindexDocumentCount = _indexStrategies[AutocompleteIndexType.KIndex]?.Count(),
            CompanyDataStamp = LatestIndexStamp(AutocompleteIndexType.Company),
            CompanyDocumentCount = _indexStrategies[AutocompleteIndexType.Company]?.Count(),
            UptimeServerDataStamp = LatestIndexStamp(AutocompleteIndexType.Uptime),
            UptimeServerDocumentCount = _indexStrategies[AutocompleteIndexType.Uptime]?.Count(),
            FullAutocompleteDataStamp = LatestIndexStamp(AutocompleteIndexType.Full),
            FullAutocompleteDocumentCount = _indexStrategies[AutocompleteIndexType.Full]?.Count(),
            AdresyDataStamp = LatestIndexStamp(AutocompleteIndexType.Adresy),
            AdresyDocumentCount = _indexStrategies[AutocompleteIndexType.Adresy]?.Count(),
        };
    }

    private string AutocompleteIndexTypeToString(AutocompleteIndexType indexType) => indexType.ToString("G");
    
    public IEnumerable<object>? Search(AutocompleteIndexType indexType, string query, int numResults = 10, string? filter = null)
    {
        var strategy = _indexStrategies[indexType]; 
        
        if (strategy is not null)
        {
            return strategy.Search(query, numResults, filter);
        }
        return default;
    }

    /// <summary>
    /// Downloads Index from generator api
    /// </summary>
    /// <returns>True if cache is downloaded, false if nothing new to download was received</returns>
    public async Task<string?> DownloadCacheIndexAsync(AutocompleteIndexType indexType, CancellationToken cancellationToken)
    {
        var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
        var httpClient = _httpClientFactory.CreateClient();

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
            .WaitAndRetryForeverAsync(attempt => TimeSpan.FromSeconds(1));

        var type = AutocompleteIndexTypeToString(indexType);

        string lastTimeStamp = LatestIndexStamp(indexType) ?? "_null_";

        var url = $"{baseUrl}?type={type}&lastTimestamp={lastTimeStamp}";

        using var responseMessage = await retryPolicy.ExecuteAsync((cancelToken) =>
            httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken), cancellationToken);

        _ = responseMessage.EnsureSuccessStatusCode();
        if (responseMessage.StatusCode == HttpStatusCode.NoContent) // there are no newer data ready
            return null;

        await using var stream = await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        var outputDirName = responseMessage.Content.Headers.ContentDisposition?.FileName?.Replace(".zip", "");
        if (outputDirName is null)
        {
            logger.Warning("Output directory name couldnt be retrieved.",
                responseMessage.Content.Headers.ContentDisposition);
            outputDirName = DateTime.Now.ToString("0yyyyMMdd");
        }

        var outputDirPath = Path.Combine(PathToIndexFolder(indexType), outputDirName);
        zipArchive.ExtractToDirectory(outputDirPath);
        return outputDirPath;
    }

    /// <summary>
    /// Updates cache if it can. If a new Index is corrupted, then it rolls back to the old one and reports corrupted Index to Generator.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<bool> TryUpdateIndexAsync(AutocompleteIndexType indexType, string path, CancellationToken cancellationToken)
    {
        var newIndex = CreateIndex(indexType, path);

        if (newIndex.IsCorrupted())
        {
            logger.Error($"Index {AutocompleteIndexTypeToString(indexType)} on path [{path}] is corrupted.");
            await ReportCorruptedIndexAsync(path, indexType, cancellationToken);
            newIndex.Dispose();
            Directory.Delete(path, recursive: true);
            return false;
        }
        
        ReplaceOldIndex(indexType, newIndex);
        LastUpdateRun[indexType] = DateTime.Now;
        return true;
    }
    
    private void ReplaceOldIndex(AutocompleteIndexType indexType, IIndexStrategy newIndex)
    {
        var oldIndex = _indexStrategies[indexType];
        _indexStrategies[indexType] = newIndex;
        oldIndex?.Dispose();
    }
    
    private IIndexStrategy CreateIndex(AutocompleteIndexType indexType, string path)
    {
        return indexType switch
        {
            AutocompleteIndexType.Adresy => new IndexStrategy<AdresyKVolbam>(path, results => results),
            AutocompleteIndexType.Uptime => new IndexStrategy<StatniWebyAutocomplete>(path, results => results),
            AutocompleteIndexType.KIndex => new IndexStrategy<SubjectNameCache>(path, results => results
                .OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Name).Length)),
            AutocompleteIndexType.Company => new IndexStrategy<Autocomplete>(path, results => results
                .OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)),
            AutocompleteIndexType.Full => new IndexStrategy<Autocomplete>(path, results => results
                .OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)),
            _ => throw new ArgumentOutOfRangeException(nameof(indexType), indexType, null)
        };
    }
    
    public async Task RollbackIndexAsync(AutocompleteIndexType indexType, CancellationToken cancellationToken)
    {
        try
        {
            //get current index folder
            string? path = PathToLatestIndex(indexType);
            if (path is null || !Directory.EnumerateFiles(path).Any())
            {
                logger.Warning("Prazdny index folder.");
                return;
            }

            _indexStrategies[indexType]?.Dispose();
            Directory.Delete(path, recursive: true);
            
            logger.Debug($"Reporting corrupted {indexType:G} index.");
            await ReportCorruptedIndexAsync(path, indexType, cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            
            logger.Debug($"Loading older {indexType:G} index.");
            path = PathToLatestIndex(indexType);
            if (path is null || !Directory.EnumerateFiles(path).Any())
            {
                logger.Warning("Prazdny index folder.");
                return;
            }
            var isUpdated = await TryUpdateIndexAsync(indexType, path, cancellationToken);
            if(isUpdated)
                return;
            
            logger.Debug($"Try download newer {indexType:G} index.");
            path = await DownloadCacheIndexAsync(indexType, cancellationToken);
            if (path is null || !Directory.EnumerateFiles(path).Any())
            {
                logger.Warning("Prazdny index folder.");
                return;
            }
            logger.Debug($"Updating {indexType:G} index.");
            await TryUpdateIndexAsync(indexType, path, cancellationToken);
        }
        catch (Exception e)
        {
            logger.Error($"Error occured during index load.", e);
            
        }
    }

    private async Task ReportCorruptedIndexAsync(string path, AutocompleteIndexType indexType, CancellationToken cancellationToken)
    {
        var yymmddPart = path.Split(Path.DirectorySeparatorChar,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?
            .Last();

        var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
        var httpClient = _httpClientFactory.CreateClient();

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(attempt * 50));


        var type = AutocompleteIndexTypeToString(indexType);
        var url = $"{baseUrl}?type={type}&name={yymmddPart}";

        using var responseMessage = await retryPolicy.ExecuteAsync((cancelToken) =>
            httpClient.DeleteAsync(url, cancelToken), cancellationToken);

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            logger.Warning($"Pravděpodobně došlo k selhání při promazávání vadných dat u generátoru. " +
                           $"Url [{url}], response [{responseMessage.StatusCode} - {responseMessage.ReasonPhrase}]");
        }
    }
    
    private static string PathToIndexFolder(AutocompleteIndexType type)
    {
        string path = Path.Combine(Devmasters.Config.GetWebConfigValue("DataFolder"), type.ToString("G"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string? PathToLatestIndex(AutocompleteIndexType type)
    {
        var directories = Directory.EnumerateDirectories(PathToIndexFolder(type));
        return directories.MaxBy(f => f);
    }

    private static string? LatestIndexStamp(AutocompleteIndexType type)
    {
        return PathToLatestIndex(type)?
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?
            .Last();
    }

    public void DeleteOldCache()
    {
        foreach (var indexType in Enum.GetValues<AutocompleteIndexType>())
        {
            var directories = Directory.EnumerateDirectories(PathToIndexFolder(indexType));
            var dirsToDelete = directories.OrderByDescending(f => f).Skip(3);

            foreach (var dirToDelete in dirsToDelete)
            {
                Directory.Delete(dirToDelete, true);
            }
        }
    }
}

public class Status
{
    public Dictionary<AutocompleteIndexType,DateTime?> LastUpdates { get; set; }
    public string? KindexDataStamp { get; set; }
    public int? KindexDocumentCount { get; set; }
    public string? CompanyDataStamp { get; set; }
    public int? CompanyDocumentCount { get; set; }
    public string? UptimeServerDataStamp { get; set; }
    public int? UptimeServerDocumentCount { get; set; }
    public string? FullAutocompleteDataStamp { get; set; }
    public int? FullAutocompleteDocumentCount { get; set; }
    public string? AdresyDataStamp { get; set; }
    public int? AdresyDocumentCount { get; set; }
}