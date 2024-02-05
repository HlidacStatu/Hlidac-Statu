using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    // private readonly ILogger<HomeController> _logger;
   
    public async Task<IActionResult> Index()
    {
        ViewData["platy"] = await PuRepo.GetPlatyAsync(Util.DefaultYear);
        
        return View();
    }
    
    public async Task<IActionResult> Oblast(string id)
    {
        var organizace = await PuRepo.GetOrganizacForOblasteAsync(id);
        
        ViewData["platy"] = organizace.SelectMany(o => o.Platy).ToList();;
        ViewData["oblast"] = id;

        List<Breadcrumb> breadcrumbs = PuOrganizace.PathSplitter(id)
            .Select(kvp => new Breadcrumb()
            {
                Name = kvp.Key,
                Action = nameof(Oblast),
                Id = kvp.Value
            }).ToList();

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = id;

        return View(organizace);
    }

    public async Task<IActionResult> Detail(int id, int rok = Util.DefaultYear )
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);
        ViewData["platy"] = detail.Platy.ToList();
        
        List<Breadcrumb> breadcrumbs = detail.OblastPath()
            .Select(kvp => new Breadcrumb()
            {
                Name = kvp.Key,
                Action = nameof(Oblast),
                Id = kvp.Value,
            }).ToList();
        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.Nazev,
            Action = nameof(Detail),
            Id = id.ToString(),
            Year = rok
        });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = detail.Nazev;

        return View(detail);
    }
    public async Task<IActionResult> Detail2(int id, int? rok = null)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);

        ViewData["platy"] = detail.Platy.ToList();
        ViewData["rok"] = rok ?? (detail.Platy.Any() ? detail.Platy.Max(m=>m.Rok) : Util.DefaultYear);
        ViewData["id"] = id;
        
        List<Breadcrumb> breadcrumbs = detail.OblastPath()
            .Select(kvp => new Breadcrumb()
            {
                Name = kvp.Key,
                Action = nameof(Oblast),
                Id = kvp.Value,
            }).ToList();
        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.Nazev,
            Action = nameof(Detail),
            Id = id.ToString(),
            Year = rok
        });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = detail.Nazev;
        
        return View(detail);
    }

    public async Task<IActionResult> Plat(int id)
    {
        var detail = await PuRepo.GetPlatAsync(id);
        
        List<Breadcrumb> breadcrumbs = detail.Organizace.OblastPath()
            .Select(kvp => new Breadcrumb()
            {
                Name = kvp.Key,
                Action = nameof(Oblast),
                Id = kvp.Value
            }).ToList();
        breadcrumbs.Add(new Breadcrumb()
            {
                Name = detail.Organizace.Nazev,
                Action = nameof(Detail),
                Id = detail.Organizace.Id.ToString(),
                Year = detail.Rok
            });
        breadcrumbs.Add(new Breadcrumb()
            {
                Name = detail.NazevPozice,
                Action = nameof(Plat),
                Id = id.ToString(),
                Year = detail.Rok
            });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = $"{detail.NazevPozice} v organizaci {detail.Organizace.Nazev}";
        
        return View(detail);
    }

}