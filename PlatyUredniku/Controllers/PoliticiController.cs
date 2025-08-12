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
        var fullPoliticiViewDataTask = _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            _ => GetFullPoliticiViewData()
        );
        var politickeStranyFilter = _cache.GetOrSet<List<string>>(
            "politickeStranyFilter",
            _ => GetPolitickeStranyForFilter()
        );

        var fullPoliticiViewData = await fullPoliticiViewDataTask;

        var maxJobCount = (int)fullPoliticiViewData.Max(x => x.PocetJobu) + 1;
        var maxTotalIncome = (int)fullPoliticiViewData.Max(x => x.CelkovyRocniPrijem) + 1;

        // parties + "Ostatní"
        var parties = politickeStranyFilter.ToList();
        if (!parties.Contains("Ostatní", StringComparer.InvariantCultureIgnoreCase))
            parties.Add("Ostatní");

        var model = new SeznamFilterModel
        {
            DataEndpointUrl = Url.Action(nameof(SeznamData), "Politici")!,
            InitialData = fullPoliticiViewData,
            Filters = new List<FilterField>
            {
                new ChoiceFilterField
                {
                    Key = "politicianGroups",
                    Label = "Politická role",
                    Multiple = false,
                    Options = new List<FilterOption>
                    {
                        new() { Value = "všichni", Label = "Všichni" },
                        new() { Value = "předseda vlády" },
                        new() { Value = "ministr" },
                        new() { Value = "poslanec" },
                        new() { Value = "senátor" },
                        new() { Value = "europoslanec" },
                        new() { Value = "hejtman" },
                        new() { Value = "krajský zastupitel" }
                    },
                    Initial = new[] { "všichni" }
                },
                new RangeFilterField
                {
                    Key = "totalIncome",
                    Label = "Celkový roční příjem",
                    Min = 0,
                    Max = maxTotalIncome,
                    Step = 10_000,
                    Initial = (0, maxTotalIncome),
                    Unit = "Kč"
                },
                new RangeFilterField
                {
                    Key = "jobCount",
                    Label = "Počet jobů",
                    Min = 0,
                    Max = maxJobCount,
                    Step = 1,
                    Initial = (0, maxJobCount)
                },
                new ChoiceFilterField
                {
                    Key = "party",
                    Label = "Politická strana",
                    Multiple = true,
                    Options = parties.Select(p => new FilterOption { Value = p, Label = p }).ToList(),
                    Initial = parties.ToArray() // start with all selected incl. "Ostatní"
                },
                new ChoiceFilterField
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
        var fullPoliticiViewDataTask = _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            _ => GetFullPoliticiViewData()
        );
        var politickeStranyFilter = _cache.GetOrSet<List<string>>(
            "politickeStranyFilter",
            _ => GetPolitickeStranyForFilter()
        );

        var resultData = await fullPoliticiViewDataTask;
        var q = HttpContext.Request.Query;

        // Read generic filters (names come from your FilterField.Key values)
        var genders = q.Choices("gender"); // ["m","f"] or ["muž","žena"] depending on your UI
        var (incomeFrom, incomeTo) = q.RangeDecimal("totalIncome");
        var (jobsFrom, jobsTo) = q.RangeInt("jobCount");
        var parties = q.Choices("party");
        var groups = q.Choices("politicianGroups");

        IEnumerable<PoliticiViewData> filtered = resultData;

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

            filtered = filtered.Where(d => norm.Contains(d.Pohlavi));
        }

        // Ranges
        if (incomeFrom.HasValue) filtered = filtered.Where(d => d.CelkovyRocniPrijem >= incomeFrom.Value);
        if (incomeTo.HasValue) filtered = filtered.Where(d => d.CelkovyRocniPrijem <= incomeTo.Value);
        if (jobsFrom.HasValue) filtered = filtered.Where(d => d.PocetJobu >= jobsFrom.Value);
        if (jobsTo.HasValue) filtered = filtered.Where(d => d.PocetJobu <= jobsTo.Value);

        // Party including "Ostatní" handling
        if (parties.Length > 0)
        {
            var partiesSet = new HashSet<string>(parties, StringComparer.InvariantCultureIgnoreCase);
            filtered = filtered.Where(d =>
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
            filtered = filtered.Where(d =>
                groups.Any(g => d.PolitickaRole.Contains(g, StringComparison.InvariantCultureIgnoreCase)));
        }

        return new JsonResult(filtered.ToList(), new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<List<PoliticiViewData>> GetFullPoliticiViewData()
    {
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

    private List<string> GetPolitickeStranyForFilter()
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