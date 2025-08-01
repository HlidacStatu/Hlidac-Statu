using HlidacStatu.Entities;
using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;
using PlatyUredniku.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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
        var platyTask = _cache.GetOrSetAsync<Dictionary<string, PpPrijem[]>>(
            $"{nameof(PpRepo.GetPlatyGroupedByNameIdAsync)}_{rok}-politici",
            _ => PpRepo.GetPlatyGroupedByNameIdAsync(rok)
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


    public async Task<IActionResult> Reporty()
    {
        return View();
    }


    [Route("Politici/Report/{id}")]
    public async Task<IActionResult> Report(string id)
    {
        return View("reporty/report"+id);
    }


    [HlidacCache(60 * 60, "*")]
    public async Task<IActionResult> Seznam(string id, int? year)
    {
        if (!Enum.TryParse<PpRepo.PoliticianGroup>(id, out var politicianGroup))
        {
            politicianGroup = PpRepo.PoliticianGroup.Vse;
        }

        return View((Group: politicianGroup, Year: year ?? PpRepo.DefaultYear));
    }

    [HlidacCache(48*60 * 60, "*")]
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
}