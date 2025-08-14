using HlidacStatu.Entities;
using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HlidacStatu.Extensions;
using HlidacStatu.Util;
using PlatyUredniku.DataTable;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin")]
public partial class PoliticiController : Controller
{
    private readonly IFusionCache _cache;

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
            return View();

        var detail = await _cache.GetOrSetAsync<List<PpPrijem>>(
            $"{nameof(PpRepo.GetPrijmyPolitika)}_{id}-politici",
            _ => PpRepo.GetPrijmyPolitika(id)
        );

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

    [HlidacCache(48 * 60 * 60, "*")]
    public async Task<IActionResult> VsechnyOrganizace(int rok = PpRepo.DefaultYear)
    {
        ViewData["rok"] = rok;

        var organizaceViewData = await GetFullOrganizaceViewDataCached(rok);
        var maxPocetPlatu = organizaceViewData.Max(o => o.PocetPlatu);
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
                    Initial = ["vše"],
                    Multiple = false,
                    Options =
                    [
                        new() { Value = "vše", Label = "vše" },
                        new() { Value = "success", Label = "platy dodaly" },
                        new() { Value = "primary", Label = "neodpověděli" },
                        new() { Value = "danger", Label = "žádost zamítli" },
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

    [HlidacCache(60 * 60, "*")]
    public async Task<IActionResult> Seznam()
    {
        var fullPoliticiViewData = await GetFullPoliticiViewDataCached();
        var politickeStranyFilterData = GetPolitickeStranyForFilterCached();

        var maxJobCount = (int)fullPoliticiViewData.Max(x => x.PocetJobu) + 1;
        var maxTotalIncomeInMilions =
            Math.Ceiling((fullPoliticiViewData.Max(x => x.CelkoveRocniNaklady) + 1) / 1_000_000);

        var filteredPoliticiViewData = FilterPoliticiViewData(fullPoliticiViewData, politickeStranyFilterData);

        // parties + "Ostatní"
        var parties = politickeStranyFilterData;
        if (!parties.Contains("vše")) parties.Insert(0, "vše");
        if (!parties.Contains("Ostatní")) parties.Add("Ostatní");

        // Initialize filter
        var model = new DataTableFilter
        {
            DataEndpointUrl = Url.Action(nameof(SeznamData), "Politici")!,
            InitialData = filteredPoliticiViewData,
            DefaultOrder = "[[3, 'desc']]",
            Filters = new List<DataTableFilters.FilterField>
            {
                new DataTableFilters.ChoiceFilterField
                {
                    Key = PoliticiFilterKeys.PoliticianGroups,
                    Label = "Politická role",
                    Multiple = false,
                    Options =
                    [
                        new() { Value = "všichni", Label = "Všichni" },
                        new() { Value = "předseda vlády" },
                        new() { Value = "ministr" },
                        new() { Value = "poslanec" },
                        new() { Value = "senátor" },
                        // new() { Value = "europoslanec" },
                        new() { Value = "hejtman" },
                        new() { Value = "krajský zastupitel" }
                    ],
                    Initial = ["všichni"]
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
                    Initial = ["vše"]
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
            filteredData = filteredData.Where(d => d.CelkoveRocniNaklady >= incomeFrom.Value * 1_000_000m);
        if (incomeTo.HasValue)
            filteredData = filteredData.Where(d => d.CelkoveRocniNaklady <= incomeTo.Value * 1_000_000m);
        if (jobsFrom.HasValue) filteredData = filteredData.Where(d => d.PocetJobu >= jobsFrom.Value);
        if (jobsTo.HasValue) filteredData = filteredData.Where(d => d.PocetJobu <= jobsTo.Value);

        // Party including "Ostatní" handling
        if (parties.Length > 0 && !parties.Contains("vše", StringComparer.InvariantCultureIgnoreCase))
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
        if (groups.Length > 0 && !groups.Contains("všichni", StringComparer.InvariantCultureIgnoreCase))
        {
            filteredData = filteredData.Where(d =>
                groups.Any(g => d.PolitickaRole.Contains(g, StringComparison.InvariantCultureIgnoreCase)));
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
        
        if (platCountFrom.HasValue) filteredData = filteredData.Where(d => d.PocetPlatu >= platCountFrom.Value);
        if (platCountTo.HasValue) filteredData = filteredData.Where(d => d.PocetPlatu <= platCountTo.Value);
        if (maxPlatFrom.HasValue) filteredData = filteredData.Where(d => d.PlatyDo >= maxPlatFrom.Value * 1_000_000);
        if (maxPlatTo.HasValue) filteredData = filteredData.Where(d => d.PlatyDo <= maxPlatTo.Value * 1_000_000);

        // Groups
        if (stavy.Length > 0 && !stavy.Contains("vše", StringComparer.InvariantCultureIgnoreCase))
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
                    var osoba = OsobaRepo.GetByNameId(politikPlatyKvp.Key);

                    politiciViewData.Add(new PoliticiViewData()
                    {
                        CelkoveRocniNaklady = politikPlatyKvp.Value.Sum(p => p.CelkoveRocniNakladyNaPolitika),
                        Politik = $"<a href='/politici/politik/{osoba.NameId}'>{osoba.FullName()}</a>",
                        Politik_Sort = $"{osoba.Prijmeni}-{osoba.Jmeno}",
                        PocetJobu = politikPlatyKvp.Value.Length,
                        Pohlavi = osoba.Pohlavi,
                        PolitickaRole = osoba.MainRolesToString(PpRepo.DefaultYear),
                        PolitickaStrana =
                            osoba.CurrentPoliticalParty(), //todo: změnit na politickou stranu v konkrétním roce (přidat funkčnost)
                        Organizace = "<ol>" + string.Join("", politikPlatyKvp.Value
                            .Select(n => n.Organizace)
                            .Distinct()
                            .Select(o => $"<li><a href='/politici/organizace/{o?.DS}'>{o?.Nazev}</a></li>")
                        ) + "</ol>",
                    });
                }

                return politiciViewData;
            }
        );

        return fullPoliticiViewDataTask;
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

                var platyMin = (await PpRepo.GetJednotlivePlatyAsync(rok)).Min(m => m.CelkoveRocniNakladyNaPolitika);
                var platyMax = (await PpRepo.GetJednotlivePlatyAsync(rok)).Max(m => m.CelkoveRocniNakladyNaPolitika);
                var platyLength = platyMax - platyMin;
                if (platyLength == 0)
                    platyLength = 0.01m;

                return orgs.Select(o =>
                {
                    var org = new OrganizaceViewData()
                    {
                        NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>",
                        EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok),
                        PocetPlatu = 0
                    };

                    if (!o.PrijmyPolitiku.Any())
                    {
                        return org;
                    }

                    var minPlat = o.PrijmyPolitiku.Min(p => p.CelkoveRocniNakladyNaPolitika);
                    var maxPlat = o.PrijmyPolitiku.Max(p => p.CelkoveRocniNakladyNaPolitika);
                    var startPer = Math.Round((minPlat - platyMin) / platyLength * 100);
                    var endPer = Math.Round((maxPlat - minPlat) / platyLength * 100);
                    if (endPer == 0)
                        endPer = 1;

                    org.PlatyOd = minPlat;
                    org.PlatyDo = maxPlat;
                    org.PocetPlatu = o.PrijmyPolitiku.Count();
                    org.NazevOrganizace = $"<a href='/politici/organizace/{o.Ico}'>{o.Nazev}</a>";
                    org.EventStatus = o.PlatyForYearPoliticiDescriptionHtml(rok);
                    org.Graf = $"""
                                <div class="d-flex"
                                     style="width: 100%;height: 1rem;background: linear-gradient(90deg, hsl(216deg 100% 87%) 0%, hsl(216deg 100% 26.67%) 100%);">
                                    <div class="border-start  bg-light" style="width: {startPer}%;height: 1rem"></div>
                                    <div class="bg-transparent rounded-pill position-relative"
                                         style="width: {endPer}%;height: 1rem;">
                                    </div>
                                    <div class="border-end bg-light" style="width: {100 - endPer - startPer}%;height: 1rem"></div>
                                </div> 
                                """;
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

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická role")]
        public string PolitickaRole { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Price, "Roční příjem + náhrady")]
        public Decimal CelkoveRocniNaklady { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet jobů")]
        public Decimal PocetJobu { get; set; }

        public string Organizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Pohlaví")]
        public string Pohlavi { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická strana")]
        public string PolitickaStrana { get; set; }
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

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet platů")]
        public Decimal PocetPlatu { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyOd")]
        public Decimal PlatyOd { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "PlatyDo")]
        public Decimal PlatyDo { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Rozpětí platů")]
        public string Platy =>
            $"{RenderData.NicePrice(PlatyOd).Replace(" ", "&nbsp;")}-{RenderData.NicePrice(PlatyDo).Replace(" ", "&nbsp;")}";

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Platy sort")]
        public decimal Platy_Sort => PlatyDo;

        public string Graf { get; set; }
    }

    public static class OrganizaceFilterKeys
    {
        public const string PocetPlatu = "pocetPlatu";
        public const string Stav = "stav";
        public const string MaxPlat = "maxPlat";
    }
}