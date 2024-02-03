using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    // private readonly ILogger<HomeController> _logger;
   
    public async Task<IActionResult> Index()
    {
        ViewData["platy"] = await PuRepo.GetPlatyAsync(2022);
        
        return View();
    }
    
    public async Task<IActionResult> Oblast(string id)
    {
        var organizace = await PuRepo.GetOrganizacForOblasteAsync(id);
        
        ViewData["platy"] = organizace.SelectMany(o => o.Platy).ToList();;
        ViewData["oblast"] = id;

        return View(organizace);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);
        ViewData["platy"] = detail.Platy.ToList();;

        return View(detail);
    }
    public async Task<IActionResult> Detail2(int id, int? rok = null)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);

        ViewData["platy"] = detail.Platy.ToList(); ;
        ViewData["rok"] = rok ?? (detail.Platy.Any() ? detail.Platy.Max(m=>m.Rok) : Util.DefaultYear);
        ViewData["id"] = id;

        return View(detail);
    }

    public async Task<IActionResult> Plat(int id)
    {
        var detail = await PuRepo.GetPlatAsync(id);
        
        return View(detail);
    }

}