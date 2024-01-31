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
    
    public async Task<IActionResult> Oblast(string oblast)
    {
        var organizace = await PuRepo.GetPlatyAsync(oblast);
        
        ViewData["platy"] = organizace.SelectMany(o => o.Platy).ToList();;
        ViewData["oblast"] = oblast;

        return View(organizace);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);
        
        ViewData["platy"] = detail.Platy.ToList();;
        
        return View(detail);
    } 

}