namespace PoliticiEditor.Components.Autocomplete;

public class AutocompleteBackgroundTimer : BackgroundService
{
    private readonly LocallyCachedAutocompleteService _locallyCachedAutocompleteService;

    public AutocompleteBackgroundTimer(LocallyCachedAutocompleteService locallyCachedAutocompleteService)
    {
        _locallyCachedAutocompleteService = locallyCachedAutocompleteService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromDays(1));

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
        var cacheTask = _locallyCachedAutocompleteService.RefreshAutocompleteDataAsync(stoppingToken);

        await Task.WhenAll(cacheTask);
        Console.WriteLine("Refresh autocompletu dokončen.");
    }
}