using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HlidacStatu.Extensions.DataTables;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.Repositories.Cache;

namespace PlatyUredniku.Controllers;

public partial class PoliticiController : Controller
{
    private const string FilterAll = "Vše";
    
    [HlidacOutputCache(60 * 60, "*")]
    public async Task<IActionResult> List(string id, int? year, int? top = null, string sort = null,
        string report = null)
    {
        if (!Enum.TryParse<PpRepo.PoliticianGroup>(id, true, out var politicianGroup))
        {
            politicianGroup = PpRepo.PoliticianGroup.Vse;
        }

        return View((Group: politicianGroup, Year: year ?? PpRepo.DefaultYear, top: top ?? int.MaxValue - 1, sort: sort,
            report: report));
    }

    [HlidacOutputCache(60 * 60, "rok")]
    public async Task<IActionResult> Index(int rok = PpRepo.DefaultYear)
    {
        //titulka politiku

        return View();
    }


    [HlidacOutputCache(60 * 60, "id;rok")]
    public async Task<IActionResult> Politik(string id, int rok = PpRepo.DefaultYear)
    {
        //detail politika

        ViewBag.Title = $"Platy politika {id}";
        var osoba = await OsobaCache.GetPersonByNameIdAsync(id);
        if (osoba is null)
            return NotFound();

        var detail = await PpRepo.Cached.GetPrijmyPolitikaAsync(id, rok);
        
        if (detail == null || detail.Count == 0)
        {
            //no data
            return Redirect(osoba.GetUrl(false));
        }
        ViewData["osoba"] = osoba;

        return View(detail);
    }

    [HlidacOutputCache(48 * 60 * 60, "*")]
    public async Task<IActionResult> Organizace(string id, int rok = PpRepo.DefaultYear)
    {
        ViewBag.rok = rok;
        PuOrganizace detail = null;
        if (HlidacStatu.Util.DataValidators.CheckCZICO(id))
        {
            //ico
            detail = await PpRepo.GetOrganizaceFullDetailPerIcoAsync(id);
        }
        else if (!string.IsNullOrEmpty(id))
        {
            //datovka
            detail = await PpRepo.GetOrganizaceFullDetailAsync(id);
        }

        if (detail == null)
            return RedirectToAction(nameof(VsechnyOrganizace));
        else
            return View(detail);
    }

    public async Task<IActionResult> VsechnyOrganizace(int rok = PpRepo.DefaultYear)
    {
        var organizaceViewData = await HlidacStatu.Extensions.Cache.Platy.Politici.GetFullOrganizaceViewDataCachedAsync(rok);
        var maxPocetPlatu = organizaceViewData.Max(o => o.PocetOsob);
        var maxPlatInMils = Math.Ceiling((organizaceViewData.Max(x => x.PlatyDo) + 1) / 1_000_000);
        
        var filteredOrganizaceViewData = FilterOrganizaceViewData(organizaceViewData);
        
        
        var model = new DataTableFilter
        {
            DataEndpointUrl = Url.Action(nameof(VsechnyOrganizaceData), "Politici") + $"?rok={rok}",
            InitialData = filteredOrganizaceViewData,
            DefaultOrder = "[[3,'desc']]",
            Filters = new List<DataTableFilters.FilterField>
            {
                new DataTableFilters.RangeFilterField
                {
                    Key = OrganizaceFilterKeys.MaxPlat,
                    Label = "Rozmezí vrchního platu",
                    Min = 0,
                    Max = maxPlatInMils,
                    Step = 0.5m,
                    Initial = (0, maxPlatInMils),
                    Unit = "mil. Kč"
                },
                new DataTableFilters.RangeFilterField
                {
                    Key = OrganizaceFilterKeys.PocetPlatu,
                    Label = "Počet platů",
                    Min = 0,
                    Max = maxPocetPlatu,
                    Step = 1,
                    Initial = (0, maxPocetPlatu)
                },
                new DataTableFilters.ChoiceFilterField()
                {
                    Key = OrganizaceFilterKeys.Stav,
                    Label = "Stav žádosti",
                    Initial = [FilterAll],
                    Multiple = false,
                    Options =
                    [
                        new() { Value = FilterAll, Label = FilterAll },
                        new() { Value = "success", Label = "Poskytli všechny platy" },
                        new() { Value = "warning", Label = "Poskytli část platů" },
                        new() { Value = "danger", Label = "Neposkytli žádný plat" },
                    ]
                },
            }
        };


        return View(model);
    }

    public async Task<IActionResult> VsechnyOrganizaceData(int rok = PpRepo.DefaultYear)
    {
        var organizaceViewData = await HlidacStatu.Extensions.Cache.Platy.Politici.GetFullOrganizaceViewDataCachedAsync(rok);
        var filteredOrganizaceViewData = FilterOrganizaceViewData(organizaceViewData);

        return new JsonResult(filteredOrganizaceViewData.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    
    public async Task<IActionResult> Seznam(string report = "platy", int rok = PuRepo.DefaultYear)
    {
        var fullPoliticiViewData = await HlidacStatu.Extensions.Cache.Platy.Politici.GetFullPoliticiViewDataCachedAsync(rok);
        var politickeStranyFilterData = await PpRepo.Cached.GetPolitickeStranyForFilterAsync();

        var maxJobCount = (int)fullPoliticiViewData.Max(x => x.PocetJobu) + 1;
        var maxTotalIncomeInMilions =
            Math.Ceiling((fullPoliticiViewData.Max(x => x.CelkoveRocniNaklady_Sort) + 1) / 1_000_000);

        var filteredPoliticiViewData = FilterPoliticiViewDataAsync(rok, HttpContext.Request.Query, politickeStranyFilterData);

        // parties + "Ostatní"
        var parties = politickeStranyFilterData;
        if (!parties.Contains(FilterAll)) parties.Insert(0, FilterAll);
        if (!parties.Contains("Ostatní")) parties.Add("Ostatní");

        string sorting = "[[3, 'desc']]";
        if (report == "funkce")
        {
            sorting = "[[4,'desc']]";
        }
        
        // Initialize filter
        var model = new DataTableFilter
        {
            DataEndpointUrl = Url.Action(nameof(SeznamData), "Politici")!,
            InitialData = filteredPoliticiViewData,
            DefaultOrder = sorting,
            Filters = new List<DataTableFilters.FilterField>
            {
                new DataTableFilters.ChoiceFilterField
                {
                    Key = PoliticiFilterKeys.Year,
                    Label = "Rok",
                    Multiple = false,
                    Hidden = true,
                    Options =
                    [
                        new() { Value = PuRepo.DefaultYear.ToString(), Label = PuRepo.DefaultYear.ToString() },
                    ],
                    Initial = [PuRepo.DefaultYear.ToString()]
                },
                new DataTableFilters.ChoiceFilterField
                {
                    Key = PoliticiFilterKeys.PoliticianGroups,
                    Label = "Politická role",
                    Multiple = false,
                    Options =
                    [
                        new() { Value = FilterAll, Label = FilterAll },
                        new() { Value = "člen vlády" },
                        // new() { Value = "ministr" },
                        new() { Value = "poslanec" },
                        new() { Value = "senátor" },
                        // new() { Value = "europoslanec" },
                        new() { Value = "hejtman" },
                        new() { Value = "krajský zastupitel" }
                    ],
                    Initial = [FilterAll]
                },
                new DataTableFilters.RangeFilterField
                {
                    Key = PoliticiFilterKeys.TotalIncome,
                    Label = "Celkový roční příjem",
                    Min = 0,
                    Max = maxTotalIncomeInMilions,
                    Step = 0.5m,
                    Initial = (0, maxTotalIncomeInMilions),
                    Unit = "mil. Kč"
                },
                new DataTableFilters.RangeFilterField
                {
                    Key = PoliticiFilterKeys.JobCount,
                    Label = "Počet příjmů",
                    Min = 0,
                    Max = maxJobCount,
                    Step = 1,
                    Initial = (0, maxJobCount)
                },
                new DataTableFilters.ChoiceFilterField
                {
                    Key = PoliticiFilterKeys.Party,
                    Label = "Politická strana",
                    Multiple = false,
                    Options = parties.Select(p => new DataTableFilters.FilterOption { Value = p, Label = p }).ToList(),
                    Initial = [FilterAll]
                },
                new DataTableFilters.ChoiceFilterField
                {
                    Key = PoliticiFilterKeys.Gender,
                    Label = "Pohlaví",
                    Multiple = true,
                    Options =
                    [
                        new() { Value = "m", Label = "muž" },
                        new() { Value = "f", Label = "žena" }
                    ],
                    Initial = ["m", "f"]
                }

            }
        };

        return View(model);
    }

    private string[]? SetInitialGroupsForFilter()
    {
        var q = HttpContext.Request.Query;
        
        var groups = q.Choices(PoliticiFilterKeys.PoliticianGroups);

        if (groups.Length > 0)
        {
            return groups;

        }
        
        return [FilterAll];
    }

    //todo: Prasečina odsud až úplně dolů - to bude potřeba refaktorovat
    public async Task<IActionResult> SeznamData()
    {

        var politickeStranyFilterData = await PpRepo.Cached.GetPolitickeStranyForFilterAsync();

        var filtered = await FilterPoliticiViewDataAsync(HttpContext.Request.Query, politickeStranyFilterData);

        return new JsonResult(filtered.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    // Filtruje data podle query stringu
    private async Task<List<HlidacStatu.Extensions.Cache.Views.PoliticiViewData>> FilterPoliticiViewDataAsync(Microsoft.AspNetCore.Http.IQueryCollection query,
        List<string> politickeStranyFilter)
    {
        Microsoft.AspNetCore.Http.IQueryCollection q = query;
        string? year = q.Choices(PoliticiFilterKeys.Year)?.FirstOrDefault();

        var rok = Devmasters.ParseText.ToInt(year) ?? PuRepo.DefaultYear;   
        return await FilterPoliticiViewDataAsync(rok, query, politickeStranyFilter);
    }

    private async Task<List<HlidacStatu.Extensions.Cache.Views.PoliticiViewData>> FilterPoliticiViewDataAsync(int rok,
        Microsoft.AspNetCore.Http.IQueryCollection query,
        List<string> politickeStranyFilter)
    {
        Microsoft.AspNetCore.Http.IQueryCollection q = query;

        // Read generic filters (names come from your FilterField.Key values)
        var genders = q.Choices(PoliticiFilterKeys.Gender); // ["m","f"] or ["muž","žena"] depending on your UI
        var (incomeFrom, incomeTo) = q.RangeDecimal(PoliticiFilterKeys.TotalIncome);
        var (jobsFrom, jobsTo) = q.RangeInt(PoliticiFilterKeys.JobCount);
        var parties = q.Choices(PoliticiFilterKeys.Party);
        var groups = q.Choices(PoliticiFilterKeys.PoliticianGroups);



        IEnumerable<HlidacStatu.Extensions.Cache.Views.PoliticiViewData> filteredData =
            await HlidacStatu.Extensions.Cache.Platy.Politici.GetFullPoliticiViewDataCachedAsync(rok);

        // Gender
        if (genders.Length > 0)
        {
            // Accept either UI sending "m"/"f" or "muž"/"žena"
            var norm = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var g in genders)
            {
                if (string.Equals(g, "muž", StringComparison.InvariantCultureIgnoreCase)) norm.Add("m");
                else if (string.Equals(g, "žena", StringComparison.InvariantCultureIgnoreCase)) norm.Add("f");
                else norm.Add(g); // assume already "m"/"f"
            }

            filteredData = filteredData.Where(d => norm.Contains(d.Pohlavi));
        }

        // Ranges
        if (incomeFrom.HasValue)
            filteredData = filteredData.Where(d => d.CelkoveRocniNaklady_Sort >= incomeFrom.Value * 1_000_000m);
        if (incomeTo.HasValue)
            filteredData = filteredData.Where(d => d.CelkoveRocniNaklady_Sort <= incomeTo.Value * 1_000_000m);
        if (jobsFrom.HasValue) filteredData = filteredData.Where(d => d.PocetJobu >= jobsFrom.Value);
        if (jobsTo.HasValue) filteredData = filteredData.Where(d => d.PocetJobu <= jobsTo.Value);

        // Party including "Ostatní" handling
        if (parties.Length > 0 && !parties.Contains(FilterAll, StringComparer.InvariantCultureIgnoreCase))
        {
            var partiesSet = new HashSet<string>(parties, StringComparer.InvariantCultureIgnoreCase);
            filteredData = filteredData.Where(d =>
            {
                if (partiesSet.Contains(d.PolitickaStrana)) return true;

                // Known party but not selected
                if (politickeStranyFilter.Contains(d.PolitickaStrana, StringComparer.InvariantCultureIgnoreCase))
                    return false;

                // Unknown party falls into "Ostatní"
                return partiesSet.Contains("Ostatní");
            });
        }

        // Groups
        if (groups.Length > 0 && !groups.Contains(FilterAll, StringComparer.InvariantCultureIgnoreCase))
        {
            filteredData = filteredData.Where(d =>
                groups.Any(g => 
                    g.Equals("člen vlády", StringComparison.InvariantCultureIgnoreCase) ?
                        d.PolitickeRoleFilter.Any(r => r.StartsWith("ministr", StringComparison.InvariantCultureIgnoreCase) 
                                                       || r.StartsWith("předseda vlády", StringComparison.InvariantCultureIgnoreCase) ) :
                        d.PolitickeRoleFilter.Any(r => r.StartsWith(g, StringComparison.InvariantCultureIgnoreCase))));
            
        }

        return filteredData.ToList();
    }
    
    private List<HlidacStatu.Extensions.Cache.Views.OrganizaceViewData> FilterOrganizaceViewData(List<HlidacStatu.Extensions.Cache.Views.OrganizaceViewData> fullPoliticiViewDataTask)
    {
        var resultData = fullPoliticiViewDataTask;
        var q = HttpContext.Request.Query;

        // Read generic filters (names come from your FilterField.Key values)
        var (platCountFrom, platCountTo) = q.RangeInt(OrganizaceFilterKeys.PocetPlatu);
        var (maxPlatFrom, maxPlatTo) = q.RangeInt(OrganizaceFilterKeys.MaxPlat);
        var stavy = q.Choices(OrganizaceFilterKeys.Stav);

        IEnumerable<HlidacStatu.Extensions.Cache.Views.OrganizaceViewData> filteredData = resultData;
        
        if (platCountFrom.HasValue) filteredData = filteredData.Where(d => d.PocetOsob >= platCountFrom.Value);
        if (platCountTo.HasValue) filteredData = filteredData.Where(d => d.PocetOsob <= platCountTo.Value);
        if (maxPlatFrom.HasValue) filteredData = filteredData.Where(d => d.PlatyDo >= maxPlatFrom.Value * 1_000_000);
        if (maxPlatTo.HasValue) filteredData = filteredData.Where(d => d.PlatyDo <= maxPlatTo.Value * 1_000_000);

        // Groups
        if (stavy.Length > 0 && !stavy.Contains(FilterAll, StringComparer.InvariantCultureIgnoreCase))
        {
            filteredData = filteredData.Where(d =>
                stavy.Any(s => d.EventStatus.Contains(s, StringComparison.InvariantCultureIgnoreCase)));
        }

        return filteredData.ToList();
    }
    
    public static class PoliticiFilterKeys
    {
        public const string PoliticianGroups = "politicianGroups";
        public const string TotalIncome = "totalIncome";
        public const string JobCount = "jobCount";
        public const string Party = "party";
        public const string Gender = "gender";
        public const string Year = "year";
    }

    public static class OrganizaceFilterKeys
    {
        public const string PocetPlatu = "pocetPlatu";
        public const string Stav = "stav";
        public const string MaxPlat = "maxPlat";
    }
}