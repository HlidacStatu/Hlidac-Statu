using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Entities;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    private readonly IFusionCache _cache;

    public HomeController(IFusionCache cache)
    {
        _cache = cache;
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
        (int Min, int Max) range = id switch
        {
            2 => (20_000, 40_000),
            3 => (40_000, 70_000),
            4 => (70_000, 1000_000_000),
            _ => (0, 20_000)
        };
        
        var platyTask = _cache.GetOrSetAsync<List<PuPlat>>(
            $"{nameof(PuRepo.GetPoziceDlePlatuAsync)}_{range.Min}_{range.Max}_{PuRepo.DefaultYear}",
            _ => PuRepo.GetPoziceDlePlatuAsync(range.Min, range.Max, PuRepo.DefaultYear)
        );
        
        var platy = await platyTask;

        ViewData["platy"] = platy;
        ViewData["context"] = $"Dle platů {range.Min} - {range.Max},-";
        
        return View(platy);
    }

    public async Task<IActionResult> Oblast(string id)
    {
        var organizaceForTagTask = _cache.GetOrSetAsync<List<PuOrganizace>>(
            $"{nameof(PuRepo.GetOrganizaceForTagAsync)}_{id}",
            _ => PuRepo.GetOrganizaceForTagAsync(id)
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
                $"{nameof(PuRepo.GetOrganizaceForTagAsync)}_{oblast}",
                _ => PuRepo.GetOrganizaceForTagAsync(oblast)
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
        var detail = await _cache.GetOrSetAsync<PuOrganizace>(
            $"{nameof(PuRepo.GetFullDetailAsync)}_{id}",
            _ => PuRepo.GetFullDetailAsync(id)
        );

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
        
        ViewData["context"] = $"{detail.NazevPozice} v organizaci {detail.Organizace.FirmaDs.DsSubjName}";

        return View(detail);
    }

    public IActionResult Statistiky()
    {
        return View();
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