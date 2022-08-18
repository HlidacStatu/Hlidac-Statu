using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VolebniPrukaz.Models;
using VolebniPrukaz.Services;

namespace VolebniPrukaz.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AutocompleteCache _autocompleteCache;


    public HomeController(ILogger<HomeController> logger, AutocompleteCache autocompleteCache)
    {
        _logger = logger;
        _autocompleteCache = autocompleteCache;
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    public JsonResult FindAddress([FromQuery]string query, CancellationToken ctx)
    {
        var index = _autocompleteCache.GetIndex();
        var result = index.Search(query, 10, adr => adr.TypOvm );
        return Json(result.Select(x => x.Original).ToList());
    }
}