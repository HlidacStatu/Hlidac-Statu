using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.LibCore.Enums;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace HlidacStatu.AutocompleteApi.Services;

public class TimedHostedService : BackgroundService
{
    private ILogger logger = Log.ForContext<TimedHostedService>();
    private IndexCache _indexCache;

    public TimedHostedService(IndexCache indexCache, IHttpClientFactory httpClientFactory)
    {
        _indexCache = indexCache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(8));

        // This is important so this background task wont hold server from starting.
        await Task.Yield();

        do
        {
            logger.Debug($"Timer triggered.");
            await RunLoad(stoppingToken);

            await Task.Delay(TimeSpan.FromMinutes(3),
                stoppingToken); // waits so that some possible stuck requests frees old indexes

            _indexCache.DeleteOldCache();
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunLoad(CancellationToken stoppingToken)
    {
        var downloadAndUpdateTasks = Enum.GetValues<AutocompleteIndexType>()
            .Select(it => DownloadAndUpdateAsync(it, stoppingToken));

        await Task.WhenAll(downloadAndUpdateTasks);
        
    }

    private async Task DownloadAndUpdateAsync(AutocompleteIndexType indexType, CancellationToken cancellationToken)
    {
        try
        {
            logger.Debug($"Loading {indexType:G} index.");
            var path = await _indexCache.DownloadCacheIndexAsync(indexType, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(path) || !Directory.EnumerateFiles(path).Any())
            {
                logger.Warning("Empty index folder.");
                return;
            }
            
            logger.Debug($"Updating {indexType:G} index.");
            await _indexCache.TryUpdateIndexAsync(indexType, path, cancellationToken);
        }
        catch (Exception e)
        {
            logger.Error($"Error occured during index load.", e);
            
        }
    }
}