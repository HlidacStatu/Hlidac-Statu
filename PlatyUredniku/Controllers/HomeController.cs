using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    // private readonly ILogger<HomeController> _logger;
   
    public IActionResult Index()
    {
        return View();
    }
    
    public async Task<IActionResult> Oblast(string id)
    {
        var organizace = await PuRepo.GetPlatyAsync(id);
        
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
    
    public async Task<IActionResult> Plat(int id)
    {
        var detail = await PuRepo.GetPlatAsync(id);
        
        return View(detail);
    }

}