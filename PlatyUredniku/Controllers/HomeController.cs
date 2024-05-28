using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    private readonly IFusionCache _cache;

    public HomeController(IFusionCache cache)
    {
        _cache = cache;
    }
    public async Task<IActionResult> Analyza(string id)
    {

        return View("Analyza_"+id);

    }
    
    public async Task<IActionResult> Index()
    {
        var platyTask = _cache.GetOrSetAsync<List<PuPlat>>(
            $"{nameof(PuRepo.GetPlatyAsync)}_{PuRepo.DefaultYear}",
            _ => PuRepo.GetPlatyAsync(PuRepo.DefaultYear)
        );

        ViewData["platy"] = await platyTask;

        return View();
    }

    public async Task<IActionResult> DlePlatu(int id)
    {
        int prumernyPlat = 46_013;
        (int Min, int Max) range = (0, int.MaxValue);
        string title = "Manažerské platy ve veřejné správě";
        string noteHtml = "";
        switch (id)
        {
            case 2:
                range = (0_000, prumernyPlat);
                title = "Manažerské platy ve veřejné správě nižší než průměrný plat v ČR za rok 2023";
                noteHtml = "Průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a>v Q4 2023 byl <b>46 013 Kč hrubého</b>.";
                break;
            case 3:
                range = (prumernyPlat, 70_000);
                title = "Manažerské platy ve veřejné správě vyšší než průměrný plat v ČR za rok 2023";
                noteHtml = "Průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a> byl <b>46 013 Kč hrubého</b>.";
                break;
            case 4:
                range = (prumernyPlat * 2, 100_000_000);
                title = "Nejvyšší manažerské platy ve veřejné správě za rok 2023";
                noteHtml = "Zobrazujeme platy, které jsou více než dvojnásobné, než je průměrný plat v Q4 2023 <a href='https://www.czso.cz/csu/czso/cri/prumerne-mzdy-4-ctvrtleti-2023' target='_blank'>podle ČSÚ </a>(46 013 Kč hrubého).";
                break;
            case 1:
            default:
                range = (0_000, 33_000);
                title = "Manažerské platy ve veřejné správě za rok 2023 nižší než nástupní plat pokladní/ho v Lidlu ";
                noteHtml = "Nástupní plat pokladní v Lidl v Praze byl <a href='https://www.idnes.cz/ekonomika/domaci/lidl-mzdy-prodavaci-obchod-retezec.A231213_161827_ekonomika_ven' target='_blank'>podle iDnes</a> <b>33 700 Kč hrubého</b>.";
                break;
        }

        var platy = await StaticCache.GetPoziceDlePlatuAsync(range.Min, range.Max, PuRepo.DefaultYear);
        var platyCount = await StaticCache.GetPlatyCountPerYearAsync(PuRepo.DefaultYear);

        ViewData["platy"] = platy;
        //ViewData["context"] = $"Dle platů {range.Min} - {range.Max},-";
        ViewData["title"] = title;
        ViewData["noteHtml"] = noteHtml;
        ViewData["pocetPlatuCelkem"] = platyCount;
        return View(platy);
    }

    public async Task<IActionResult> Oblast(string id)
    {
        ValueTask<List<PuOrganizace>> organizaceForTagTask = _cache.GetOrSetAsync<List<PuOrganizace>>(
            $"{nameof(PuRepo.GetActiveOrganizaceForTagAsync)}_{id}",
            _ => PuRepo.GetActiveOrganizaceForTagAsync(id)
        );

        var organizace = await organizaceForTagTask;

        ViewData["platy"] = organizace.SelectMany(o => o.Platy).ToList();
        ViewData["oblast"] = id;
        ViewData["context"] = $"{id}";

        return View(organizace);
    }

    public async Task<IActionResult> Oblasti()
    {
        var sw = new Stopwatch();
        sw.Start();
        var oblasti = PuRepo.MainTags;
        var model = new Dictionary<string, List<PuOrganizace>>();
        foreach (var oblast in oblasti)
        {
            var organizace = await _cache.GetOrSetAsync<List<PuOrganizace>>(
                $"{nameof(PuRepo.GetActiveOrganizaceForTagAsync)}_{oblast}",
                _ => PuRepo.GetActiveOrganizaceForTagAsync(oblast)
            );

            model.Add(oblast, organizace);
        }

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = nameof(Oblasti),
                Link = $"{nameof(Oblasti)}"
            }
        };

        ViewData["breadcrumbs"] = breadcrumbs;
        sw.Stop();
        ViewData["sw"] = sw.ElapsedMilliseconds;
        return View(model);
    }

    public async Task<IActionResult> Detail2(string id, int rok = PuRepo.DefaultYear)
    {
        var detail = await _cache.GetOrSetAsync<PuOrganizace>(
            $"{nameof(PuRepo.GetFullDetailAsync)}_{id}",
            _ => PuRepo.GetFullDetailAsync(id)
        );

        ViewData["platy"] = detail.Platy.ToList();
        ViewData["context"] = detail.FirmaDs.DsSubjName;

        return View(detail);
    }

    public async Task<IActionResult> Detail(string id, int? rok = null)
    {
        var detail = await StaticCache.GetFullDetailAsync(id);

        ViewData["mainTag"] = detail.Tags.FirstOrDefault(t => PuRepo.MainTags.Contains(t.Tag))?.Tag;
        ViewData["platy"] = detail.Platy.ToList();
        ViewData["rok"] = rok ?? (detail.Platy.Any() ? detail.Platy.Max(m => m.Rok) : PuRepo.DefaultYear);
        ViewData["id"] = id;
        ViewData["context"] = detail.FirmaDs.DsSubjName;

        return View(detail);
    }

    public async Task<IActionResult> Plat(int id)
    {
        var detail = await _cache.GetOrSetAsync<PuPlat>(
            $"{nameof(PuRepo.GetPlatAsync)}_{id}",
            _ => PuRepo.GetPlatAsync(id)
        );

        ViewData["mainTag"] = detail.Organizace.Tags.FirstOrDefault(t => PuRepo.MainTags.Contains(t.Tag))?.Tag;
        ViewData["context"] = $"{detail.NazevPozice} v organizaci {detail.Organizace.FirmaDs.DsSubjName}";

        return View(detail);
    }

    public IActionResult Statistiky()
    {
        return View();
    }
    public IActionResult Statistika(string id, string typ)
    {
        return View("statistika_"+id,typ);
    }

    public async Task<IActionResult> Export(string type, string datovaSchranka, int? rok)
    {
        List<dynamic> data = new List<dynamic>();
        byte[] rawData = null;
        string contentType = "";
        string filename = "";

        if (rok is not null)
        {
            var platy = await PuRepo.GetPlatyWithOrganizaceForYearAsync(rok.Value);
            foreach (var plat in platy)
            {
                data.Add(plat.FlatExport());
            }

        }
        else if (!string.IsNullOrWhiteSpace(datovaSchranka))
        {
            var detail = await PuRepo.GetFullDetailAsync(datovaSchranka);
            foreach (var plat in detail.Platy)
            {
                data.Add(plat.FlatExport());
            }
        }
        else
        {
            return NoContent();
        }

        switch (type)
        {
            case "excel":
                rawData = new HlidacStatu.ExportData.Excel().ExportData(new HlidacStatu.ExportData.Data(data));
                contentType = "application/vnd.ms-excel";
                filename = "export.xlsx";
                break;
            case "tsv":
                rawData = new HlidacStatu.ExportData.TabDelimited().ExportData(new HlidacStatu.ExportData.Data(data));
                contentType = "text/tab-separated-values";
                filename = "export.tsv";
                break;
            default:
                return NoContent();
        }

        return File(rawData, contentType, filename);
    }
}