using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlatyUredniku.Services;

public class AutocompleteTimer : BackgroundService
{
    private readonly AutocompleteCacheService _autocompleteCacheService;

    public AutocompleteTimer(AutocompleteCacheService autocompleteCacheService)
    {
        _autocompleteCacheService = autocompleteCacheService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));

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
        await _autocompleteCacheService.RefreshAutocompleteDataAsync(stoppingToken);
        Console.WriteLine("Refresh autocompletu dokončen.");
    }
}