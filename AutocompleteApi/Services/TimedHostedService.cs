using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.LibCore.Enums;
using Microsoft.Extensions.Hosting;

namespace HlidacStatu.AutocompleteApi.Services;

public class TimedHostedService : BackgroundService
{
    private Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger<TimedHostedService>();
    private IndexCache _indexCache;
    private IHttpClientFactory _httpClientFactory;

    public TimedHostedService(IndexCache indexCache, IHttpClientFactory httpClientFactory)
    {
        _indexCache = indexCache;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(8));
        
        // This is important so this background task wont hold server from starting.
        await Task.Yield();

        do
        {
            logger.Debug($"Timer triggered.");
            await RunLoop(stoppingToken);
            
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunLoop(CancellationToken stoppingToken)
    {
        foreach (var indexType in Enum.GetValues<AutocompleteIndexType>())
        {
            if (Debugger.IsAttached && indexType != AutocompleteIndexType.Uptime)
                continue;

            logger.Debug($"Loading {indexType:G} index.");
            try
            {
                await _indexCache.DownloadCacheIndexAsync(indexType, _httpClientFactory);
            }
            catch (Exception e)
            {
                logger.Error($"Error occured during {indexType:G} index load.", e);
            }
        }

        _indexCache.UpdateCache();

        await Task.Delay(TimeSpan.FromMinutes(3),
            stoppingToken); // waits so that some possible stuck requests frees old indexes

        _indexCache.DeleteOldCache();
    }
}