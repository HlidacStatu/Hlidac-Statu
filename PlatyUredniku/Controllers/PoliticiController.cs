using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;
using Microsoft.AspNetCore.Authorization;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin")]
public class PoliticiController : Controller
{
    private readonly IFusionCache _cache;

    public PoliticiController(IFusionCache cache)
    {
        _cache = cache;
    }
    
    public async Task<IActionResult> Index()
    {
        var platyTask = _cache.GetOrSetAsync<List<PuPolitikPrijem>>(
            $"{nameof(PpRepo.GetPlatyAsync)}_{PpRepo.DefaultYear}",
            _ => PpRepo.GetPlatyAsync(PpRepo.DefaultYear)
        );

        ViewData["platy"] = await platyTask;

        return View();
    }

    public async Task<IActionResult> Oblast(string id)
    {
        ValueTask<List<PuOrganizace>> organizaceForTagTask = _cache.GetOrSetAsync<List<PuOrganizace>>(
            $"{nameof(PpRepo.GetActiveOrganizaceForTagAsync)}_{id}",
            _ => PpRepo.GetActiveOrganizaceForTagAsync(id)
        );

        var organizace = await organizaceForTagTask;

        ViewData["platy"] = organizace.SelectMany(o => o.PrijmyPolitiku).ToList();
        ViewData["oblast"] = id;
        ViewData["context"] = $"{id}";

        ViewBag.Title = "Prijmy politiku a organizace v oblasti #" + id;

        return View(organizace);
    }

    public async Task<IActionResult> Oblasti()
    {
        var sw = new Stopwatch();
        sw.Start();
        var oblasti = PpRepo.MainTags;
        var model = new Dictionary<string, List<PuOrganizace>>();
        foreach (var oblast in oblasti)
        {
            var organizace = await _cache.GetOrSetAsync<List<PuOrganizace>>(
                $"{nameof(PpRepo.GetActiveOrganizaceForTagAsync)}_{oblast}",
                _ => PpRepo.GetActiveOrganizaceForTagAsync(oblast)
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

        ViewBag.Title = "Prijmy politiku a organizace v různých oborech";

        return View(model);
    }

    
    public async Task<IActionResult> Detail(string id, int? rok = null)
    {
        var detail = await StaticCache.GetFullDetailAsync(id);
        
        ViewBag.Title = detail.Nazev;

        ViewData["mainTag"] = detail.Tags.FirstOrDefault(t => PpRepo.MainTags.Contains(t.Tag))?.Tag;
        ViewData["platy"] = detail.PrijmyPolitiku.ToList();
        ViewData["rok"] = rok ?? (detail.PrijmyPolitiku.Any() ? detail.PrijmyPolitiku.Max(m => m.Rok) : PpRepo.DefaultYear);
        ViewData["id"] = id;
        ViewData["context"] = detail.FirmaDs.DsSubjName;

        return View(detail);
    }

    public async Task<IActionResult> Politik(string id)
    {
        var detail = await _cache.GetOrSetAsync<List<PuPolitikPrijem>>(
            $"{nameof(PpRepo.GetPrijmyPolitika)}_{id}",
            _ => PpRepo.GetPrijmyPolitika(id)
        );

        ViewBag.Title = $"Platy politika {id}";
        var osoba = OsobaRepo.GetByNameId(id);
        if (osoba is null)
            return NotFound($"Politika {id} jsme nenašli.");
        
        
        ViewData["osoba"] = osoba;

        return View(detail);
    }
}