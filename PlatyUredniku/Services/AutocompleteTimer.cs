using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlatyUredniku.Services;

public class AutocompleteTimer : BackgroundService
{
    private readonly AutocompleteCacheService _autocompleteCacheService;
    private readonly AutocompleteCategoryCacheService _autocompleteCategoryCacheService;

    public AutocompleteTimer(AutocompleteCacheService autocompleteCacheService,
        AutocompleteCategoryCacheService autocompleteCategoryCacheService)
    {
        _autocompleteCacheService = autocompleteCacheService;
        _autocompleteCategoryCacheService = autocompleteCategoryCacheService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromHours(2));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Timed Hosted Service is stopping.");
        }
    }

    // Could also be a async method, that can be awaited in ExecuteAsync above
    private async Task RunAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Spouštím refresh autocompletu.");
        var cacheTask = _autocompleteCacheService.RefreshAutocompleteDataAsync(stoppingToken);
        var categoryCacheTask = _autocompleteCategoryCacheService.RefreshAutocompleteDataAsync(stoppingToken);

        await Task.WhenAll(cacheTask, categoryCacheTask);
        Console.WriteLine("Refresh autocompletu dokončen.");
    }
}