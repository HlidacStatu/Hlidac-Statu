using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using Whisperer;

namespace PlatyUredniku.Services;

public class AutocompleteCacheService
{
    private readonly string _tempDirectory = "AutocompleteCache";
    private bool _alternateDirectory = true;
    private Index<Autocomplete>? AutocompleteCache { get; set; }

    private string TempDirectory => $"./{_tempDirectory}_{_alternateDirectory}";

    public AutocompleteCacheService()
    {
        //cleanup
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, true);
        }
        _alternateDirectory = !_alternateDirectory;
        if (Directory.Exists(TempDirectory))
        {
            Directory.Delete(TempDirectory, true);
        }
        _alternateDirectory = !_alternateDirectory;
    }

    public IEnumerable<Autocomplete> Search(string query)
    {
        if (AutocompleteCache is null)
            return Enumerable.Empty<Autocomplete>();
        
        return AutocompleteCache.Search(query, numResults: 15)
            .OrderByDescending(s => s.Score)
            .Take(8)
            .Select(s => s.Document);
    }

    public async Task RefreshAutocompleteDataAsync(CancellationToken stoppingToken)
    {
        _alternateDirectory = !_alternateDirectory; //switch dir

        var index = new Index<Autocomplete>(TempDirectory);
        var autocompleteData = await LoadDataForAutocompleteAsync(stoppingToken);
        index.AddDocuments(autocompleteData,
            textSelector: ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
            boostSelector: bs => bs.PriorityMultiplier);

        var oldCache = AutocompleteCache;
        AutocompleteCache = index;

        await Task.Delay(5_000, stoppingToken); //safe wait until old queries are done
        oldCache?.DeleteDocuments();
        oldCache?.Dispose();
    }

    private async Task<List<Autocomplete>> LoadDataForAutocompleteAsync(CancellationToken cancellationToken)
    {
        var results = new List<Autocomplete>();

        var organizaceTask = LoadOrganizace(cancellationToken);
        var oblastTask = LoadOblasti(cancellationToken);

        await Task.WhenAll(organizaceTask, oblastTask);

        if (cancellationToken.IsCancellationRequested)
            return Enumerable.Empty<Autocomplete>().ToList();

        results.AddRange(organizaceTask.Result);
        results.AddRange(oblastTask.Result);

        return results;
    }

    private async Task<List<Autocomplete>> LoadOrganizace(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace.AsNoTracking()
            .Select(o => new Autocomplete()
            {
                Id = $"/detail/{o.Id}",
                Text = $"{o.Nazev}",
                AdditionalHiddenSearchText = $"{o.Ico} {o.DS}",
                PriorityMultiplier = 1,
                Type = "instituce",
                ImageElement = $"<i class='fas fa-university'></i>",
                Description = $"{o.Oblast}",
                Category = Autocomplete.CategoryEnum.Authority,
            })
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    private async Task<List<Autocomplete>> LoadOblasti(CancellationToken stoppingToken)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizace.AsNoTracking()
            .Select(o => o.Oblast)
            .Distinct()
            .Select(o => new Autocomplete()
            {
                Id = $"/oblast/{o}",
                Text = $"{o}",
                PriorityMultiplier = 1,
                Type = "oblast",
                ImageElement = $"<i class=\"fa-regular fa-flatbread\"></i>",
                Description = $"{o}",
                Category = Autocomplete.CategoryEnum.Oblast,
            })
            .ToListAsync(cancellationToken: stoppingToken);
    }
}