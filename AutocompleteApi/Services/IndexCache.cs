using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.LibCore.Enums;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Whisperer;

namespace HlidacStatu.AutocompleteApi.Services;

public class IndexCache
{
    public Index<SubjectNameCache>? Kindex { get; set; }
    public Index<Autocomplete>? Company { get; set; }
    public Index<StatniWebyAutocomplete>? UptimeServer { get; set; }
    public Index<Autocomplete>? FullAutocomplete { get; set; }
    public Index<AdresyKVolbam>? Adresy { get; set; }

    private Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger<IndexCache>();
    private IHttpClientFactory _httpClientFactory;

    public IndexCache(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private DateTime? LastUpdateRun { get; set; }

    public Status? Status()
    {
        if (LastUpdateRun is null)
            return null;

        return new Status()
        {
            LastUpdate = LastUpdateRun.Value,
            KindexDataStamp = CurrentIndexStamp(AutocompleteIndexType.KIndex),
            CompanyDataStamp = CurrentIndexStamp(AutocompleteIndexType.Company),
            UptimeServerDataStamp = CurrentIndexStamp(AutocompleteIndexType.Uptime),
            FullAutocompleteDataStamp = CurrentIndexStamp(AutocompleteIndexType.Full),
            AdresyDataStamp = CurrentIndexStamp(AutocompleteIndexType.Adresy)
        };
    }

    private string AutocompleteIndexTypeToString(AutocompleteIndexType indexType) =>
        indexType switch
        {
            AutocompleteIndexType.Adresy => nameof(AutocompleteIndexType.Adresy),
            AutocompleteIndexType.KIndex => nameof(AutocompleteIndexType.KIndex),
            AutocompleteIndexType.Company => nameof(AutocompleteIndexType.Company),
            AutocompleteIndexType.Uptime => nameof(AutocompleteIndexType.Uptime),
            AutocompleteIndexType.Full => nameof(AutocompleteIndexType.Full),
            _ => throw new ArgumentOutOfRangeException()
        };

    public async Task DownloadCacheIndexAsync(AutocompleteIndexType indexType)
    {
        var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
        var httpClient = _httpClientFactory.CreateClient();

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(attempt * 50));

        var type = AutocompleteIndexTypeToString(indexType);

        string lastTimeStamp = CurrentIndexStamp(indexType) ?? "_null_";

        var url = $"{baseUrl}?type={type}&lastTimestamp={lastTimeStamp}";

        using var responseMessage = await retryPolicy.ExecuteAsync(() =>
            httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));

        _ = responseMessage.EnsureSuccessStatusCode();
        if (responseMessage.StatusCode == HttpStatusCode.NoContent) // there are no newer data ready
            return;

        await using var stream = await responseMessage.Content.ReadAsStreamAsync();
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
    }


    /// <summary>
    /// Updates cache if it can. If a new Index is corrupted, then it rolls back to the old one and reports corrupted Index to Generator.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task UpdateCacheAsync()
    {
        foreach (var indexType in Enum.GetValues<AutocompleteIndexType>())
        {
            string? path = PathToCurrentIndex(indexType);
            if (path is null || !Directory.EnumerateFiles(path).Any())
            {
                logger.Warning("Prazdny index folder.");
                continue;
            }

            switch (indexType)
            {
                case AutocompleteIndexType.Adresy:
                {
                    var newIndex = new Index<AdresyKVolbam>(path);
                    if (IsIndexCorrupted(newIndex))
                    {
                        await ReportCorruptedIndexAsync(path, indexType);
                        newIndex.Dispose();
                        Directory.Delete(path, recursive:true);
                    }
                    else
                    {
                        var oldIndex = Adresy;
                        Adresy = newIndex;
                        oldIndex.Dispose();
                    }
                }
                    break;
                case AutocompleteIndexType.KIndex:
                {
                    var newIndex = new Index<SubjectNameCache>(path);
                    if (IsIndexCorrupted(newIndex))
                    {
                        await ReportCorruptedIndexAsync(path, indexType);
                        newIndex.Dispose();
                        Directory.Delete(path, recursive:true);
                    }
                    else
                    {
                        var oldIndex = Adresy;
                        Kindex = newIndex;
                        oldIndex.Dispose();
                    }
                }
                    break;
                case AutocompleteIndexType.Company:
                {
                    var newIndex = new Index<Autocomplete>(path);
                    if (IsIndexCorrupted(newIndex))
                    {
                        await ReportCorruptedIndexAsync(path, indexType);
                        newIndex.Dispose();
                        Directory.Delete(path, recursive:true);
                    }
                    else
                    {
                        var oldIndex = Adresy;
                        Company = newIndex;
                        oldIndex.Dispose();
                    }
                }
                    break;
                case AutocompleteIndexType.Uptime:
                {
                    var newIndex = new Index<StatniWebyAutocomplete>(path);
                    if (IsIndexCorrupted(newIndex))
                    {
                        await ReportCorruptedIndexAsync(path, indexType);
                        newIndex.Dispose();
                        Directory.Delete(path, recursive:true);
                    }
                    else
                    {
                        var oldIndex = Adresy;
                        UptimeServer = newIndex;
                        oldIndex.Dispose();
                    }
                }
                    break;
                case AutocompleteIndexType.Full:
                {
                    var newIndex = new Index<Autocomplete>(path);
                    if (IsIndexCorrupted(newIndex))
                    {
                        await ReportCorruptedIndexAsync(path, indexType);
                        newIndex.Dispose();
                        Directory.Delete(path, recursive:true);
                    }
                    else
                    {
                        var oldIndex = Adresy;
                        FullAutocomplete = newIndex;
                        oldIndex.Dispose();
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        LastUpdateRun = DateTime.Now;
    }

    private async Task ReportCorruptedIndexAsync(string path, AutocompleteIndexType indexType)
    {
        var yymmddPart = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)?
            .Last();
        
        var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
        var httpClient = _httpClientFactory.CreateClient();

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(attempt * 50));
        
        
        var type = AutocompleteIndexTypeToString(indexType);
        var url = $"{baseUrl}?type={type}&name={yymmddPart}";

        using var responseMessage = await retryPolicy.ExecuteAsync(() =>
            httpClient.DeleteAsync(url));

        if (responseMessage.StatusCode != HttpStatusCode.OK)
        {
            logger.Warning($"Pravděpodobně došlo k selhání při promazávání vadných dat u generátoru. " +
                           $"Url [{url}], response [{responseMessage.StatusCode} - {responseMessage.ReasonPhrase}]");
        }
        
        
    }

    private bool IsIndexCorrupted<T>(Index<T> index) where T : IEquatable<T>
    {
        try
        {
            _ = index.Search("a");
            return true;
        }
        catch (Exception e)
        {
            logger.Error($"Index {typeof(T).Name} is corrupted.", e);
            return true;
        }
    }

    private static string PathToIndexFolder(AutocompleteIndexType type)
    {
        string path = Path.Combine(Devmasters.Config.GetWebConfigValue("DataFolder"), type.ToString("G"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string? PathToCurrentIndex(AutocompleteIndexType type)
    {
        var directories = Directory.EnumerateDirectories(PathToIndexFolder(type));
        return directories.MaxBy(f => f);
    }

    private static string? CurrentIndexStamp(AutocompleteIndexType type)
    {
        return PathToCurrentIndex(type)?
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
    public DateTime LastUpdate { get; set; }
    public string? KindexDataStamp { get; set; }
    public string? CompanyDataStamp { get; set; }
    public string? UptimeServerDataStamp { get; set; }
    public string? FullAutocompleteDataStamp { get; set; }
    public string? AdresyDataStamp { get; set; }
}