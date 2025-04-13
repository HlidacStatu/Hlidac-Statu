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
using HlidacStatu.Lib.Web.UI.Attributes;

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
        var platyTask = _cache.GetOrSetAsync<List<PpPrijem>>(
            $"{nameof(PpRepo.GetPlatyAsync)}_{PpRepo.DefaultYear}-politici",
            _ => PpRepo.GetPlatyAsync(PpRepo.DefaultYear)
        );
        var platyPolitiku = await platyTask;
        ViewData["platy"] = platyPolitiku;

        return View();
    }

    public async Task<IActionResult> Oblast(string id)
    {
        ValueTask<List<PuOrganizace>> organizaceForTagTask = _cache.GetOrSetAsync<List<PuOrganizace>>(
            $"{nameof(PpRepo.GetActiveOrganizaceForTagAsync)}_{id}-politici",
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
                $"{nameof(PpRepo.GetActiveOrganizaceForTagAsync)}_{oblast}-politici",
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
        ValueTask<PuOrganizace> fullDetailTask = _cache.GetOrSetAsync<PuOrganizace>(
            $"{nameof(PpRepo.GetFullDetailAsync)}_{id}-politici",
            _ => PpRepo.GetFullDetailAsync(id)
        );

        var detail = await fullDetailTask;
        
        ViewBag.Title = detail.Nazev;

        ViewData["mainTag"] = detail.Tags.FirstOrDefault(t => PpRepo.MainTags.Contains(t.Tag))?.Tag;
        ViewData["platy"] = detail.PrijmyPolitiku.ToList();
        ViewData["rok"] = rok ?? (detail.PrijmyPolitiku.Any() ? detail.PrijmyPolitiku.Max(m => m.Rok) : PpRepo.DefaultYear);
        ViewData["id"] = id;
        ViewData["context"] = detail.FirmaDs.DsSubjName;

        return View(detail);
    }

    [HlidacCache(60 * 60, "*")]
    public async Task<IActionResult> Seznam(string ico, string start)
    {
        List<PpPrijem> platy = null;
            platy = await PpRepo.GetPlatyAsync(PpRepo.DefaultYear, true, ico);

        return View(platy);
    }

    [HlidacCache(60*60,"*")]
    public async Task<IActionResult> Organizace(string id, int rok = PpRepo.DefaultYear )
    {
        if (HlidacStatu.Util.DataValidators.CheckCZICO(id))
            return View((id, rok));

        return View("Organizace.Seznam");

    }


    public async Task<IActionResult> Politik(string id)
    {

        ViewBag.Title = $"Platy politika {id}";
        var osoba = Osoby.GetByNameId.Get(id);
        if (osoba is null)
            return NotFound($"Politika {id} jsme nenašli.");

        var detail = await _cache.GetOrSetAsync<List<PpPrijem>>(
            $"{nameof(PpRepo.GetPrijmyPolitika)}_{id}-politici",
            _ => PpRepo.GetPrijmyPolitika(id)
        );

        ViewData["osoba"] = osoba;

        return View(detail);
    }
}