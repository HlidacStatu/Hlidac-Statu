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
    public async Task<IActionResult> List(string id, int? year, int? top = null, string sort = null, string report = null)
    {
        if (!Enum.TryParse<PpRepo.PoliticianGroup>(id, true, out var politicianGroup))
        {
            politicianGroup = PpRepo.PoliticianGroup.Vse;
        }

        return View((Group: politicianGroup, Year: year ?? PpRepo.DefaultYear, top: top ?? int.MaxValue - 1, sort: sort, report: report));
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
            return View("Organizace.Seznam");
        else
            return View(detail);
    }

    [HlidacCache(60 * 60, "*")]
    public async Task<IActionResult> Seznam()
    {
        var fullPoliticiViewData = await GetFullPoliticiViewData();
        var politickeStranyFilterData = GetPolitickeStranyForFilter();

        var maxJobCount = (int)fullPoliticiViewData.Max(x => x.PocetJobu) + 1;
        var maxTotalIncome = (int)fullPoliticiViewData.Max(x => x.CelkovyRocniPrijem) + 1;

        var filteredPoliticiViewData = FilterPoliticiViewData(fullPoliticiViewData, politickeStranyFilterData);
        
        // parties + "Ostatní"
        var parties = politickeStranyFilterData;
        parties.Insert(0, "Všechny");
        parties.Add("Ostatní");

        // Initialize filter
        var model = new DataTableFilter
        {
            DataEndpointUrl = Url.Action(nameof(SeznamData), "Politici")!,
            InitialData = filteredPoliticiViewData,
            Filters = new List<DataTableFilters.FilterField>
            {
                new DataTableFilters.ChoiceFilterField
                {
                    Key = "politicianGroups",
                    Label = "Politická role",
                    Multiple = false,
                    Options =
                    [
                        new() { Value = "všichni", Label = "Všichni" },
                        new() { Value = "předseda vlády" },
                        new() { Value = "ministr" },
                        new() { Value = "poslanec" },
                        new() { Value = "senátor" },
                        new() { Value = "europoslanec" },
                        new() { Value = "hejtman" },
                        new() { Value = "krajský zastupitel" }
                    ],
                    Initial = ["všichni"]
                },
                new DataTableFilters.RangeFilterField
                {
                    Key = "totalIncome",
                    Label = "Celkový roční příjem",
                    Min = 0,
                    Max = maxTotalIncome,
                    Step = 10_000,
                    Initial = (0, maxTotalIncome),
                    Unit = "Kč"
                },
                new DataTableFilters.RangeFilterField
                {
                    Key = "jobCount",
                    Label = "Počet jobů",
                    Min = 0,
                    Max = maxJobCount,
                    Step = 1,
                    Initial = (0, maxJobCount)
                },
                new DataTableFilters.ChoiceFilterField
                {
                    Key = "party",
                    Label = "Politická strana",
                    Multiple = false,
                    Options = parties.Select(p => new DataTableFilters.FilterOption { Value = p, Label = p }).ToList(),
                    Initial = ["Všechny"]
                },
                new DataTableFilters.ChoiceFilterField
                {
                    Key = "gender",
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
        var fullPoliticiViewData = await GetFullPoliticiViewData();

        var politickeStranyFilterData = GetPolitickeStranyForFilter();
        
        var filtered = FilterPoliticiViewData(fullPoliticiViewData, politickeStranyFilterData);

        return new JsonResult(filtered.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    // Filtruje data podle query stringu
    private List<PoliticiViewData> FilterPoliticiViewData(List<PoliticiViewData> fullPoliticiViewDataTask, List<string> politickeStranyFilter)
    {
        var resultData = fullPoliticiViewDataTask;
        var q = HttpContext.Request.Query;

        // Read generic filters (names come from your FilterField.Key values)
        var genders = q.Choices("gender"); // ["m","f"] or ["muž","žena"] depending on your UI
        var (incomeFrom, incomeTo) = q.RangeDecimal("totalIncome");
        var (jobsFrom, jobsTo) = q.RangeInt("jobCount");
        var parties = q.Choices("party");
        var groups = q.Choices("politicianGroups");

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
        if (incomeFrom.HasValue) filteredData = filteredData.Where(d => d.CelkovyRocniPrijem >= incomeFrom.Value);
        if (incomeTo.HasValue) filteredData = filteredData.Where(d => d.CelkovyRocniPrijem <= incomeTo.Value);
        if (jobsFrom.HasValue) filteredData = filteredData.Where(d => d.PocetJobu >= jobsFrom.Value);
        if (jobsTo.HasValue) filteredData = filteredData.Where(d => d.PocetJobu <= jobsTo.Value);

        // Party including "Ostatní" handling
        if (parties.Length > 0 && !parties.Contains("Všechny", StringComparer.InvariantCultureIgnoreCase))
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

    private async Task<List<PoliticiViewData>> GetFullPoliticiViewData()
    {
        var fullPoliticiViewDataTask = await _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            factory: async _ => { 
                
                var politiciPlatyGroup =
                    await PpRepo.GetPrijmyGroupedByNameIdAsync(PpRepo.DefaultYear, pouzePotvrzene: true, withDetails: true);

                List<PoliticiViewData> politiciViewData = new List<PoliticiViewData>();

                foreach (var politikPlatyKvp in politiciPlatyGroup)
                {
                    var osoba = OsobaRepo.GetByNameId(politikPlatyKvp.Key);

                    politiciViewData.Add(new PoliticiViewData()
                    {
                        CelkovyRocniPrijem = politikPlatyKvp.Value.Sum(p => p.CelkovyRocniPlatVcetneOdmen),
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

    private List<string> GetPolitickeStranyForFilter()
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

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Price, "Celkový roční příjem")]
        public Decimal CelkovyRocniPrijem { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Number, "Počet jobů")]
        public Decimal PocetJobu { get; set; }

        public string Organizace { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Hidden, "Pohlaví")]
        public string Pohlavi { get; set; }

        [HtmlTableDefinition.Column(HtmlTableDefinition.ColumnType.Text, "Politická strana")]
        public string PolitickaStrana { get; set; }
    }
}