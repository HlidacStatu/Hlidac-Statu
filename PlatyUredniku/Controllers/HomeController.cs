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
        
        ViewData["oblast"] = oblast;

        return View(organizace);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);

        return View(detail);
    } 

}