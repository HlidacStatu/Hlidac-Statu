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
    
    public async Task<IActionResult> Index(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            //titulka politiku
            var platyTask = _cache.GetOrSetAsync<List<PpPrijem>>(
                $"{nameof(PpRepo.GetPlatyAsync)}_{PpRepo.DefaultYear}-politici",
                _ => PpRepo.GetPlatyAsync(PpRepo.DefaultYear)
            );
            var platyPolitiku = await platyTask;
            ViewData["platy"] = platyPolitiku;

            return View();
        } else
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

            return View("Politik",detail);
        }
    }



    public async Task<IActionResult> Oblast(string id)
    {
/*        ValueTask<List<PuOrganizace>> organizaceForTagTask = _cache.GetOrSetAsync<List<PuOrganizace>>(
            $"{nameof(PpRepo.GetActiveOrganizaceAsync)}_{id}-politici",
            _ => PpRepo.GetActiveOrganizaceAsync(id)
        );

        var organizace = await organizaceForTagTask;

        ViewData["platy"] = organizace.SelectMany(o => o.PrijmyPolitiku).ToList();
        ViewData["oblast"] = id;
        ViewData["context"] = $"{id}";

        ViewBag.Title = "Prijmy politiku a organizace v oblasti #" + id;
*/
        return View(null);// organizace);
    }

    /*
    public async Task<IActionResult> Oblasti()
    {
        var sw = new Stopwatch();
        sw.Start();
        var oblasti = PpRepo.MainTags;
        var model = new Dictionary<string, List<PuOrganizace>>();
        foreach (var oblast in oblasti)
        {
            var organizace = await _cache.GetOrSetAsync<List<PuOrganizace>>(
                $"{nameof(PpRepo.GetActiveOrganizaceAsync)}_{oblast}-politici",
                _ => PpRepo.GetActiveOrganizaceAsync(oblast)
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
    */
    
    public async Task<IActionResult> Detail(string id, int? rok = null)
    {

        PuOrganizace detail = null;
        if (HlidacStatu.Util.DataValidators.CheckCZICO(id))
        {
            //ico
            detail = await PpRepo.GetOrganizaceFullDetailPerIcoAsync(id);
            ViewData["rok"] = rok ?? (detail.PrijmyPolitiku.Any() ? detail.PrijmyPolitiku.Max(m => m.Rok) : PpRepo.DefaultYear);
        }
        else
        {
            //datovka
            detail = await PpRepo.GetOrganizaceFullDetailAsync(id);
            ViewData["rok"] = rok ?? (detail.PrijmyPolitiku.Any() ? detail.PrijmyPolitiku.Max(m => m.Rok) : PpRepo.DefaultYear);
        }

        if (detail == null)
            return NotFound($"Organizaci {id} jsme nenašli.");


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
}