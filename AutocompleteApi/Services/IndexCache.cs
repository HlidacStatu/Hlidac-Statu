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
    public Index<SubjectNameCache> Kindex { get; set; }
    public Index<Autocomplete> Company { get; set; }
    public Index<StatniWebyAutocomplete> UptimeServer { get; set; }
    public Index<Autocomplete> FullAutocomplete { get; set; }
    public Index<AdresyKVolbam> Adresy { get; set; }

    private Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger<IndexCache>();

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

    public async Task DownloadCacheIndexAsync(AutocompleteIndexType indexType,
        IHttpClientFactory httpClientFactory)
    {
        var baseUrl = Devmasters.Config.GetWebConfigValue("GeneratorUrl");
        var httpClient = httpClientFactory.CreateClient();

        AsyncRetryPolicy<HttpResponseMessage> retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(5, attempt => TimeSpan.FromMilliseconds(attempt * 50));

        var type = indexType switch
        {
            AutocompleteIndexType.Adresy => nameof(AutocompleteIndexType.Adresy),
            AutocompleteIndexType.KIndex => nameof(AutocompleteIndexType.KIndex),
            AutocompleteIndexType.Company => nameof(AutocompleteIndexType.Company),
            AutocompleteIndexType.Uptime => nameof(AutocompleteIndexType.Uptime),
            AutocompleteIndexType.Full => nameof(AutocompleteIndexType.Full),
            _ => throw new ArgumentOutOfRangeException()
        };

        string lastTimeStamp = CurrentIndexStamp(indexType) ?? "_null_";

        var url = $"{baseUrl}?type={type}&lastTimestamp={lastTimeStamp}";

        using var responseMessage = await retryPolicy.ExecuteAsync(() =>
            httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));

        _ = responseMessage.EnsureSuccessStatusCode();
        if(responseMessage.StatusCode == HttpStatusCode.NoContent) // there are no newer data ready
            return;

        await using var stream = await responseMessage.Content.ReadAsStreamAsync();
        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
        var outputDirName = responseMessage.Content.Headers.ContentDisposition?.FileName?.Replace(".zip", "");
        if (outputDirName is null)
        {
            logger.Warning("Output directory name couldnt be retrieved.", responseMessage.Content.Headers.ContentDisposition);
            outputDirName = DateTime.Now.ToString("0yyyyMMdd");
        }
        var outputDirPath = Path.Combine(PathToIndexFolder(indexType), outputDirName);
        zipArchive.ExtractToDirectory(outputDirPath);
    }


    public void UpdateCache()
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
                    Adresy = new Index<AdresyKVolbam>(path);
                    break;
                case AutocompleteIndexType.KIndex:
                    Kindex = new Index<SubjectNameCache>(path);
                    break;
                case AutocompleteIndexType.Company:
                    Company = new Index<Autocomplete>(path);
                    break;
                case AutocompleteIndexType.Uptime:
                    UptimeServer = new Index<StatniWebyAutocomplete>(path);
                    break;
                case AutocompleteIndexType.Full:
                    FullAutocomplete = new Index<Autocomplete>(path);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        LastUpdateRun = DateTime.Now;
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
            var dirsToDelete = directories.OrderByDescending(f => f).Skip(2);

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