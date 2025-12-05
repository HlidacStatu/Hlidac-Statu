using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using Microsoft.EntityFrameworkCore;
using Whisperer;

namespace PoliticiEditor.Components.Autocomplete;

public class LocallyCachedAutocompleteService
{
    private readonly string _tempDirectory = "AutocompleteCache";
    private bool _alternateDirectory = true;
    public Index<HlidacStatu.DS.Api.Autocomplete>? PolitickeStranyAutocomplete { get; private set; }
    private string TempDirectory => $"./{_tempDirectory}_{_alternateDirectory}";

    public LocallyCachedAutocompleteService()
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

    public async Task RefreshAutocompleteDataAsync(CancellationToken cancellationToken)
    {
        _alternateDirectory = !_alternateDirectory; //switch dir

        var index = new Index<HlidacStatu.DS.Api.Autocomplete>(TempDirectory);
        var autocompleteData = await LoadDataForAutocompleteAsync(cancellationToken);
        index.AddDocuments(autocompleteData,
            textSelector: ts => $"{ts.Text} {ts.AdditionalHiddenSearchText}",
            boostSelector: bs => bs.PriorityMultiplier,
            filterSelector: fs => fs.Type);

        var oldCache = PolitickeStranyAutocomplete;
        PolitickeStranyAutocomplete = index;

        await Task.Delay(5_000, cancellationToken); //safe wait until old queries are done
        oldCache?.DeleteDocuments();
        oldCache?.Dispose();
    }

    private async Task<List<HlidacStatu.DS.Api.Autocomplete>> LoadDataForAutocompleteAsync(
        CancellationToken cancellationToken)
    {
        var results = new List<HlidacStatu.DS.Api.Autocomplete>();

        var politickeStrany = await LoadPolitickeStranyAsync(cancellationToken);
        
        if (cancellationToken.IsCancellationRequested)
            return Enumerable.Empty<HlidacStatu.DS.Api.Autocomplete>().ToList();

        results.AddRange(politickeStrany);

        return results;
    }

    private async Task<List<HlidacStatu.DS.Api.Autocomplete>> LoadPolitickeStranyAsync(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        var data = await db.Firma.Where(f => f.Kod_PF == FirmaExtension.PolitickaStrana_kodPF)
            .ToListAsync(cancellationToken: cancellationToken);

        var zkratkyStran = ZkratkaStranyRepo.ZkratkyVsechStran();

        string? GetSafeZkratka(string ico)
        {
            if (zkratkyStran.TryGetValue(ico, out var result))
            {
                return result;
            }
            return null;
        }
        

        var res = data
            .Select(o => new HlidacStatu.DS.Api.Autocomplete()
            {
                Id = $"{o.ICO}",
                Text = $"{o.Jmeno}",
                AdditionalHiddenSearchText = $"{o.ICO} {GetSafeZkratka(o.ICO)}",
                PriorityMultiplier = 1,
                Type = "politickaStrana",
                Description = GetSafeZkratka(o.ICO),
            })
            .ToList();

        return res;
    }
    
    
}