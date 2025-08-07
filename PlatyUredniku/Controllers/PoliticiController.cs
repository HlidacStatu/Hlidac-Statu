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
using Microsoft.EntityFrameworkCore;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin")]
public class PoliticiController : Controller
{
    private readonly IFusionCache _cache;

    public PoliticiController(IFusionCache cache)
    {
        _cache = cache;
    }

    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> Index(int rok = PpRepo.DefaultYear)
    {
        //titulka politiku

        return View();
    }

    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> IndexAlternative(int rok = PpRepo.DefaultYear)
    {
        //titulka politiku
        var platyTask = _cache.GetOrSetAsync<Dictionary<string, PpPrijem[]>>(
            $"{nameof(PpRepo.GetPrijmyGroupedByNameIdAsync)}_{rok}-politici",
            _ => PpRepo.GetPrijmyGroupedByNameIdAsync(rok)
        );
        var platyPolitiku = await platyTask;
        ViewData["platy"] = platyPolitiku;

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


    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> Reporty()
    {
        return View();
    }


    [HlidacCache(60 * 60, "*")]
    [Route("Politici/Report/{id}")]
    public async Task<IActionResult> Report(string id)
    {
        return View("reporty/report" + id);
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
        // záměrně ignoruju vstupy a to doladíme později
        var fullPoliticiViewDataTask = _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            _ => GetFullPoliticiViewData()
        );
        
        var fullPoliticiViewData = await fullPoliticiViewDataTask;
        //definice filtrace
        PoliticiFilter politiciFilter = new PoliticiFilter()
        {
            Gender = ["muž", "žena"],
            Party = fullPoliticiViewData.Select(x => x.PolitickaStrana).Distinct()
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Order()
                .ToArray(),
            JobCount = [0, (int)fullPoliticiViewData.Select(x => x.PocetJobu).Max() + 1],
            TotalIncome = [0, (int)fullPoliticiViewData.Select(x => x.CelkovyRocniPrijem).Max() + 1],
            PoliticianGroups = [
                "všichni",
                "předseda vlády",
                "ministr",
                "poslanec",
                "senátor",
                "europoslanec",
                "hejtman",
                "krajský zastupitel"
            ],
        };
        

        return View((fullPoliticiViewData, politiciFilter));
    }

    //todo: Prasečina odsud až úplně dolů - to bude potřeba refaktorovat
    public async Task<IActionResult> SeznamData([FromQuery]PoliticiFilter filter)
    {
        //todo: přidat ještě namísto pprepo.defaultYear rok, ke kterému se data vážou (input param?/filter)
        var fullPoliticiViewDataTask = _cache.GetOrSetAsync<List<PoliticiViewData>>(
            "fullPoliticiViewData",
            _ => GetFullPoliticiViewData()
        );
        var resultData = await fullPoliticiViewDataTask;

        if (filter is not null)
        {
            var filteredPoliticiViewData = resultData
                .Where(d => filter.Gender.Any(g => g.Equals("muž", StringComparison.InvariantCultureIgnoreCase)? 
                    d.Pohlavi.Equals("m", StringComparison.InvariantCultureIgnoreCase) : 
                    d.Pohlavi.Equals("f", StringComparison.InvariantCultureIgnoreCase)))
                .Where(d => filter.Party.Contains(d.PolitickaStrana))
                .Where(d => d.CelkovyRocniPrijem >= filter.TotalIncome[0])
                .Where(d => d.CelkovyRocniPrijem <= filter.TotalIncome[1])
                .Where(d => d.PocetJobu >= filter.JobCount[0])
                .Where(d => d.PocetJobu <= filter.JobCount[1]);

            if (!filter.PoliticianGroups.Contains("všichni"))
            {
                filteredPoliticiViewData = filteredPoliticiViewData.Where(d =>
                    filter.PoliticianGroups.Any(g =>
                        d.PolitickaRole.Contains(g, StringComparison.InvariantCultureIgnoreCase)));
            }

            resultData = filteredPoliticiViewData.ToList();
        }
        
        
        return new JsonResult(resultData, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<List<PoliticiViewData>> GetFullPoliticiViewData()
    {
        var politiciPlatyGroup = await PpRepo.GetPrijmyGroupedByNameIdAsync(PpRepo.DefaultYear, pouzePotvrzene: true, withDetails: true);

        List<PoliticiViewData> politiciViewData = new List<PoliticiViewData>();
        
        foreach (var politikPlatyKvp in politiciPlatyGroup)
        {
            var osoba = OsobaRepo.GetByNameId(politikPlatyKvp.Key);
            
            politiciViewData.Add(new PoliticiViewData()
            {
                CelkovyRocniPrijem = politikPlatyKvp.Value.Sum(p => p.CelkovyRocniPlatVcetneOdmen),
                Politik = osoba.FullName(),
                PocetJobu = politikPlatyKvp.Value.Length,
                Pohlavi = osoba.Pohlavi,
                PolitickaRole = osoba.MainRolesToString(PpRepo.DefaultYear),
                PolitickaStrana = osoba.CurrentPoliticalParty(), //todo: změnit na politickou stranu v konkrétním roce (přidat funkčnost)
                Organizace = "<ol>" + string.Join("", politikPlatyKvp.Value
                    .Select(n => n.Organizace)
                    .Distinct()
                    .Select(o => $"<li><a href='/politici/organizace/{o?.DS}'>{o?.Nazev}</a></li>")
                ) + "</ol>",
                
            });
        }
        return politiciViewData;
    }

    public class PoliticiFilter
    {
        [FilterType(FilterTypes.RadioButton, "Politická role")]
        public string[] PoliticianGroups { get; set; }

        [FilterType(FilterTypes.Range, "Celkový roční příjem")]
        public int[] TotalIncome { get; set; }

        [FilterType(FilterTypes.Range, "Počet jobů")]
        public int[] JobCount { get; set; }

        [FilterType(FilterTypes.CheckBox, "Politická strana")]
        public string[] Party { get; set; }

        [FilterType(FilterTypes.CheckBox, "Pohlaví")]
        public string[] Gender { get; set; }
    }
    
    public class PoliticiViewData
    {
        public string Politik { get; set; }
        
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

internal class FilterTypeAttribute : Attribute
{
    public FilterTypes Type { get; }
    public string Name { get; }

    public FilterTypeAttribute(FilterTypes type, string name)
    {
        Type = type;
        Name = name;
    }
}

internal enum FilterTypes
{
    RadioButton,
    Range,
    CheckBox
}

public class HtmlTableDefinition
{
    public class ColumnAttribute : Attribute
    {
        public ColumnType Type { get; }
        public string Name { get; }

        public ColumnAttribute(ColumnType type, string name)
        {
            Type = type;
            Name = name;
        }
    }

    public enum ColumnType
    {
        Text,
        Number,
        Price,
        Hidden
    }
}