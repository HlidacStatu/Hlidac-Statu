using HlidacStatu.Entities;
using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using PlatyUredniku.DataTable;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin,BetaTester")]
public partial class PoliticiController : Controller
{
    private readonly IFusionCache _cache;
    private const string FilterAll = "Vše";

    public PoliticiController(IFusionCache cache)
    {
        _cache = cache;
    }

    [HlidacCache(60 * 60, "*")]
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

    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> Index(int rok = PpRepo.DefaultYear)
    {
        //titulka politiku

        return View();
    }


    [HlidacCache(60 * 60, "id;rok")]
    public async Task<IActionResult> Politik(string id, int rok = PpRepo.DefaultYear)
    {
        //detail politika

        ViewBag.Title = $"Platy politika {id}";
        var osoba = Osoby.GetByNameId.Get(id);
        if (osoba is null)
            return NotFound();

        var detail = await _cache.GetOrSetAsync<List<PpPrijem>>(
            $"{nameof(PpRepo.GetPrijmyPolitika)}_{id}-politici",
            _ => PpRepo.GetPrijmyPolitika(id)
        );
        if (detail == null || detail.Count == 0)
        {
            //no data
            return Redirect(osoba.GetUrl(false));
        }
        ViewData["osoba"] = osoba;

        return View(detail);
    }

    [HlidacCache(48 * 60 * 60, "*")]
    public async Task<IActionResult> Organizace(string id, int rok = PpRepo.DefaultYear)
    {
        ViewData["rok"] = rok;
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
        ViewData["rok"] = rok;

        var organizaceViewData = await GetFullOrganizaceViewDataCached(rok);
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
        var organizaceViewData = await GetFullOrganizaceViewDataCached(rok);
        var filteredOrganizaceViewData = FilterOrganizaceViewData(organizaceViewData);

        return new JsonResult(filteredOrganizaceViewData.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
    
    public async Task<IActionResult> Seznam(string report = "platy")
    {
        var fullPoliticiViewData = await GetFullPoliticiViewDataCached();
        var politickeStranyFilterData = GetPolitickeStranyForFilterCached();

        var maxJobCount = (int)fullPoliticiViewData.Max(x => x.PocetJobu) + 1;
        var maxTotalIncomeInMilions =
            Math.Ceiling((fullPoliticiViewData.Max(x => x.CelkoveRocniNaklady_Sort) + 1) / 1_000_000);

        var filteredPoliticiViewData = FilterPoliticiViewData(fullPoliticiViewData, politickeStranyFilterData);

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
                    Label = "Roční příjem + náhrady",
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
        var fullPoliticiViewData = await GetFullPoliticiViewDataCached();

        var politickeStranyFilterData = GetPolitickeStranyForFilterCached();

        var filtered = FilterPoliticiViewData(fullPoliticiViewData, politickeStranyFilterData);

        return new JsonResult(filtered.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    // Filtruje data podle query stringu
    private List<PoliticiViewData> FilterPoliticiViewData(List<PoliticiViewData> fullPoliticiViewDataTask,
        List<string> politickeStranyFilter)
    {
        var resultData = fullPoliticiViewDataTask;
        var q = HttpContext.Request.Query;

        // Read generic filters (names come from your FilterField.Key values)
        var genders = q.Choices(PoliticiFilterKeys.Gender); // ["m","f"] or ["muž","žena"] depending on your UI
        var (incomeFrom, incomeTo) = q.RangeDecimal(PoliticiFilterKeys.TotalIncome);
        var (jobsFrom, jobsTo) = q.RangeInt(PoliticiFilterKeys.JobCount);
        var parties = q.Choices(PoliticiFilterKeys.Party);
        var groups = q.Choices(PoliticiFilterKeys.PoliticianGroups);

        IEnumerable<PoliticiViewData> filteredData = resultData;

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
    
    private List<OrganizaceViewData> FilterOrganizaceViewData(List<OrganizaceViewData> fullPoliticiViewDataTask)
    {
        var resultData = fullPoliticiViewDataTask;
        var q = HttpContext.Request.Query;

        // Read generic filters (names come from your FilterField.Key values)
        var (platCountFrom, platCountTo) = q.RangeInt(OrganizaceFilterKeys.PocetPlatu);
        var (maxPlatFrom, maxPlatTo) = q.RangeInt(OrganizaceFilterKeys.MaxPlat);
        var stavy = q.Choices(OrganizaceFilterKeys.Stav);

        IEnumerable<OrganizaceViewData> filteredData = resultData;
        
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

    private async Task<List<PoliticiViewData>> GetFullPoliticiViewDataCached()
    {
        var fullPoliticiViewDataTask = await _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            factory: async _ =>
            {
                var politiciPlatyGroup =
                    await PpRepo.GetPrijmyGroupedByNameIdAsync(PpRepo.DefaultYear, pouzePotvrzene: true,
                        withDetails: true);

                List<PoliticiViewData> politiciViewData = new List<PoliticiViewData>();

                foreach (var politikPlatyKvp in politiciPlatyGroup)
                {
                    var nameid = politikPlatyKvp.Key;
                    var platy = politikPlatyKvp.Value; 
                    var osoba = OsobaRepo.GetByNameId(nameid);
                    var celkoveNaklady = platy.Sum(p => p.CelkoveRocniNakladyNaPolitika);
                    
                    politiciViewData.Add(new PoliticiViewData()
                    {
                        CelkoveRocniNaklady = RenderCelkoveRocniNaklady(platy),
                        CelkoveRocniNaklady_Sort = celkoveNaklady,
                        Politik = $"<a href='/politici/politik/{osoba.NameId}'>{WebUtility.HtmlEncode(osoba.FullName())}</a>",
                        Politik_Sort = $"{osoba.Prijmeni}-{osoba.Jmeno}",
                        PocetJobu = platy.Length,
                        Pohlavi = osoba.Pohlavi,
                        PolitickeRoleFilter = osoba.MainRoles(PpRepo.DefaultYear),
                        PolitickaStrana =
                            osoba.CurrentPoliticalParty(), //todo: změnit na politickou stranu v konkrétním roce (přidat funkčnost)
                        Organizace = RenderOrganizace(platy),
                    });
                }

                return politiciViewData;
            }
        );

        return fullPoliticiViewDataTask;
    }

    private string RenderCelkoveRocniNaklady(PpPrijem[] platy)
    {
        var rottenOrgs = platy.Where(p => p.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
            .Select(n => n.Organizace)
            .Distinct()
            .ToList();
        var celkoveNaklady = platy.Sum(p => p.CelkoveRocniNakladyNaPolitika);
        var nicePrice = RenderData.NicePrice(celkoveNaklady, html: true);

        if (rottenOrgs.Any())
        {
            var result = $"""
                          <span class="help-tooltip" data-bs-toggle="tooltip" data-bs-html="true" title="Částka zahrnuje jen příjmy, které se nám podařilo shromáždit a které nám byly poskytnuty.">
                            {nicePrice}
                          </span> 
                          """;
            return result;
        }

        return nicePrice;
    }
    
    private string RenderOrganizace(PpPrijem[] platy)
    {
        var goodOrgs = platy.Where(p => p.Status > PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
            .Select(n => n.Organizace)
            .Distinct()
            .ToList();
        var rottenOrgs = platy.Where(p => p.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106)
            .Select(n => n.Organizace)
            .Distinct()
            .ToList();
        
        var goodOrgsHtmlList = string.Join("", goodOrgs
            .Select(o => $"<li><a href='/politici/organizace/{o?.DS}'>{WebUtility.HtmlEncode(o?.Nazev)}</a></li>"));
        
        var rottenOrgsHtmlList = string.Join("", rottenOrgs
            .Select(o => $"""
                          <li>
                          <a href="/politici/organizace/{o?.DS}">{WebUtility.HtmlEncode(o?.Nazev)}</a>
                          <i class="text-danger fas fa-exclamation-circle" data-bs-toggle="tooltip" title="Plat či odměna nebyla poskytnuta."></i>
                          </li>
                          """));

        return "<ol>" + goodOrgsHtmlList + rottenOrgsHtmlList + "</ol>";
    }

    private async Task<List<OrganizaceViewData>> GetFullOrganizaceViewDataCached(int rok)
    {
        var fullOrganizaceViewData = await _cache.GetOrSetAsync<List<OrganizaceViewData>>(
            $"fullOrganizaceViewData_{rok}",
            factory: async _ =>
            {
                var orgs = await PpRepo.GetActiveOrganizaceAsync(rok);
                // var allEventsPoskytnuto = await PpRepo.GetAllEventsAsync(rok,
                //     m =>m.DotazovanaInformace == PuEvent.DruhDotazovaneInformace.Politik 
                //         && m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace || m.Typ == PuEvent.TypUdalosti.PoskytnutiInformace
                // );

                return orgs.Select(o =>
                {
                    var org = new OrganizaceViewData()
                    {
                        NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>",
                        EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok),
                        PocetOsob = 0
                    };

                    if (!o.PrijmyPolitiku.Any())
                    {
                        return org;
                    }
                    
                    org.PlatyOd = o.PrijmyPolitiku.Min(p => p.CelkoveRocniNakladyNaPolitika);
                    org.PlatyDo = o.PrijmyPolitiku.Max(p => p.CelkoveRocniNakladyNaPolitika);
                    org.PocetOsob = o.PrijmyPolitiku.Count();
                    org.NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>";
                    org.EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok);
                    return org;
                }).ToList();
            }
        );

        return fullOrganizaceViewData;
    }

    private List<string> GetPolitickeStranyForFilterCached()
    {
        return _cache.GetOrSet<List<string>>(
            "politickeStranyFilterData",
            _ =>
            {
                List<string> politickeStranyIca =
                [
                    "71443339",
                    "00442704",
                    "00496936",
                    "16192656",
                    "71339698",
                    "10742409",
                    "17085438",
                    "00409171",
                    "04134940",
                    "26673908",
                    "00409740",
                    "71339728",
                    "08288909"
                ];

                return politickeStranyIca.Select(ZkratkaStranyRepo.NazevStranyForIco).ToList();
            }
        );
    }

    public class PoliticiViewData
    {
        public string Politik { get; set; }
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PolitikSort")]
        public string Politik_Sort { get; set; }
        
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická strana")]
        public string PolitickaStrana { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická role")]
        public string PolitickaRole => string.Join(", ", PolitickeRoleFilter);
        
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PolitRoleFilter")]
        public List<string> PolitickeRoleFilter {get; set;}

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Roční příjem + náhrady")]
        public string CelkoveRocniNaklady { get; set; }
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Prijem sort")]
        public Decimal CelkoveRocniNaklady_Sort { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet jobů")]
        public Decimal PocetJobu { get; set; }

        public string Organizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Pohlaví")]
        public string Pohlavi { get; set; }

        
    }

    public static class PoliticiFilterKeys
    {
        public const string PoliticianGroups = "politicianGroups";
        public const string TotalIncome = "totalIncome";
        public const string JobCount = "jobCount";
        public const string Party = "party";
        public const string Gender = "gender";
    }

    public class OrganizaceViewData
    {
        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Název organizace")]
        public string NazevOrganizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Stav")]
        public string EventStatus { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet osob")]
        public Decimal PocetOsob { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyOd")]
        public Decimal PlatyOd { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyDo")]
        public Decimal PlatyDo { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Rozpětí platů")]
        public string Platy =>
            $"{RenderData.NicePrice(PlatyOd).Replace(" ", "&nbsp;")}-{RenderData.NicePrice(PlatyDo).Replace(" ", "&nbsp;")}";

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Platy sort")]
        public decimal Platy_Sort => PlatyDo;
    }

    public static class OrganizaceFilterKeys
    {
        public const string PocetPlatu = "pocetPlatu";
        public const string Stav = "stav";
        public const string MaxPlat = "maxPlat";
    }
}