using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Extensions;
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

    public IEnumerable<Autocomplete> Search(string query, string? filter = null)
    {
        if (AutocompleteCache is null)
            return [];
        
        return AutocompleteCache.Search(query, numResults: 15, filter)
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
            boostSelector: bs => bs.PriorityMultiplier,
            filterSelector: fs => fs.Type);

        var oldCache = AutocompleteCache;
        AutocompleteCache = index;

        await Task.Delay(5_000, cancellationToken); //safe wait until old queries are done
        oldCache?.DeleteDocuments();
        oldCache?.Dispose();
    }

    private async Task<List<Autocomplete>> LoadDataForAutocompleteAsync(CancellationToken cancellationToken)
    {
        var results = new List<Autocomplete>();

        var organizaceTask = LoadOrganizace(cancellationToken);
        var oblastTask = LoadOblasti(cancellationToken);
        var ceoTask = LoadCeos(cancellationToken);
        var synonymsTask = LoadSynonyms(cancellationToken);
        var politiciTask = LoadPolitici(cancellationToken);

        await Task.WhenAll(organizaceTask, oblastTask, ceoTask, synonymsTask, politiciTask);

        if (cancellationToken.IsCancellationRequested)
            return Enumerable.Empty<Autocomplete>().ToList();

        results.AddRange(organizaceTask.Result);
        results.AddRange(oblastTask.Result);
        results.AddRange(ceoTask.Result);
        results.AddRange(synonymsTask.Result);
        results.AddRange(politiciTask.Result);

        return results;
    }

    private async Task<List<Autocomplete>> LoadOrganizace(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        var data = await db.PuOrganizace.AsNoTracking()
            .Include(o => o.FirmaDs)
            .Include(o => o.Tags)
            .ToListAsync(cancellationToken: cancellationToken);

        data = data.Where(m => m.Tags.Any()).ToList();
        var res = data
            .Select(o => new Autocomplete()
            {
                Id = $"/detail/{o.DS}",
                Text = $"{o.Nazev}",
                AdditionalHiddenSearchText = $"{o.Ico} {o.DS}",
                PriorityMultiplier = 1,
                Type = "instituce",
                ImageElement = $"<i class='fas fa-university'></i>",
                Description = "", //puvodne $"{o.Oblast}" //TODO zmenit na tagy?
                Category = Autocomplete.CategoryEnum.Authority,
            })
            .ToList();
        
        return res;
    }
    
    private async Task<List<Autocomplete>> LoadOblasti(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        return await db.PuOrganizaceTags.AsNoTracking()
            .Select(t => t.Tag)
            .Distinct()
            .Select(tag => new Autocomplete()
            {
                Id = $"/oblast/{PuOrganizaceTag.NormalizeTag(tag)}",
                Text = $"{tag}",
                AdditionalHiddenSearchText = $"tag # oblast {tag}",
                PriorityMultiplier = 1.2f,
                Type = "oblast",
                ImageElement = $"<i class=\"fa-duotone fa-hashtag\"></i>",
                Description = $"Vyhledat všechny instituce s tagem #{tag}",
                Category = Autocomplete.CategoryEnum.Oblast,
            })
            .ToListAsync(cancellationToken: cancellationToken);
    }
    
    private async Task<List<Autocomplete>> LoadCeos(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        var platy = await db.PuPlaty.AsNoTracking()
            .Where(p => p.JeHlavoun == true)
            .Where(p => p.Rok == PuRepo.DefaultYear)
            .Include( p => p.Organizace)
            .ThenInclude(o => o.FirmaDs)
            .Where(p => p.Organizace.FirmaDs.Ico != null)
            .ToListAsync(cancellationToken: cancellationToken);

        DateTime fromDate = new DateTime(PuRepo.DefaultYear, 1, 1);
        DateTime toDate = new DateTime(PuRepo.DefaultYear, 12, 31);

        var results = new List<Autocomplete>();
        foreach (var plat in platy)
        {
            if(cancellationToken.IsCancellationRequested)
                break;
            
            if(string.IsNullOrWhiteSpace(plat.Organizace.FirmaDs.Ico))
                continue;

            var autocomplete = OsobaEventRepo.GetCeos(plat.Organizace.FirmaDs.Ico, fromDate, toDate)
                .Select(o => new Autocomplete()
                {
                    Id = $"/plat/{plat.Id}",
                    Text = $"{o.Osoba.Jmeno} {o.Osoba.Prijmeni}{Osoba.AppendTitle(o.Osoba.TitulPred, o.Osoba.TitulPo)}",
                    PriorityMultiplier = 1,
                    Type = "osoba",
                    ImageElement = $"<img src='{o.Osoba.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />",
                    Description = $"{plat.Organizace.Nazev} - {plat.NazevPozice}",
                    Category = Autocomplete.CategoryEnum.Person,
                })
                .ToList();
            results.AddRange(autocomplete);
        }

        return results;
    }
    
    private static async Task<List<Autocomplete>> LoadSynonyms(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        var synonyma = await db.AutocompleteSynonyms.AsNoTracking()
            .Where(p => p.QueryPlaty != null)
            .Where(p => p.Active == 1)
            .ToListAsync(cancellationToken: cancellationToken);

        var results = synonyma.Select(s => new Autocomplete()
        {
            Id = $"/detail/{s.QueryPlaty}",
            Text = s.Text,
            Type = "instituce",
            PriorityMultiplier = 1,
            ImageElement = $"<i class='fas fa-university'></i>",
            Description = "", //puvodne $"{o.Oblast}" //TODO zmenit na tagy?
            Category = Autocomplete.CategoryEnum.Synonym
        }).ToList();
        
        return results;
    }
    
    private async Task<List<Autocomplete>> LoadPolitici(CancellationToken cancellationToken)
    {
        await using var db = new DbEntities();

        var nameIds = await db.PpPrijmy.AsNoTracking()
            .Where(p => p.Status == PpPrijem.StatusPlatu.Potvrzen)
            .Select(p => p.Nameid)
            .Distinct()
            .ToListAsync(cancellationToken: cancellationToken);

        var results = new List<Autocomplete>();
        foreach (var nameId in nameIds)
        {
            if(cancellationToken.IsCancellationRequested)
                break;

            var osoba = OsobaRepo.GetByNameId(nameId);

            var autocomplete = new Autocomplete()
            {
                Id = $"/politici/politik/{osoba.NameId}",
                Text = $"{osoba.Jmeno} {osoba.Prijmeni}{Osoba.AppendTitle(osoba.TitulPred, osoba.TitulPo)}",
                PriorityMultiplier = 1.3f,
                Type = "politik",
                ImageElement = $"<img src='{osoba.GetPhotoUrl(false, Osoba.PhotoTypes.NoBackground)}' />",
                Description = $"{osoba.MainRoles(PpRepo.DefaultYear)}",
                Category = Autocomplete.CategoryEnum.Person,
            };
            
            results.Add(autocomplete);
        }

        return results;
    }
}