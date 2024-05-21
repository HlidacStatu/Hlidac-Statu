using HlidacStatu.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MinimalEntities;
using Whisperer;

namespace PlatyUredniku.Services;

public class AutocompleteCategoryCacheService
{
    private readonly string _tempDirectory = "AutocompleteCategoryCache";
    private bool _alternateDirectory = true;
    private Index<Autocomplete>? AutocompleteCategoryCache { get; set; }

    private string TempDirectory => $"./{_tempDirectory}_{_alternateDirectory}";

    public AutocompleteCategoryCacheService()
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
        if (AutocompleteCategoryCache is null)
            return Enumerable.Empty<Autocomplete>();
        
        return AutocompleteCategoryCache.Search(query, numResults: 15)
            .OrderByDescending(s => s.Score)
            .Take(8)
            .Select(s => s.Document);
    }

    public async Task RefreshAutocompleteDataAsync(CancellationToken cancellationToken)
    {
        _alternateDirectory = !_alternateDirectory; //switch dir

        var index = new Index<Autocomplete>(TempDirectory);
        var autocompleteData = await LoadDataForAutocompleteAsync(cancellationToken);
        index.AddDocuments(autocompleteData,
            textSelector: ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
            boostSelector: bs => bs.PriorityMultiplier);

        var oldCache = AutocompleteCategoryCache;
        AutocompleteCategoryCache = index;

        await Task.Delay(5_000, cancellationToken); //safe wait until old queries are done
        oldCache?.DeleteDocuments();
        oldCache?.Dispose();
    }

    private async Task<List<Autocomplete>> LoadDataForAutocompleteAsync(CancellationToken cancellationToken)
    {
        var results = await LoadData(cancellationToken);

        return results;
    }

    private async Task<List<Autocomplete>> LoadData(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        return await db.PuCZISCO
            .AsNoTracking()
            .Where(m => m.MameVydelky)
            .Select(m => new Autocomplete()
            {
                Id = m.Kod,
                Text = m.Nazev

            })
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    
}