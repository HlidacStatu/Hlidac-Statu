using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PlatyUredniku.Controllers;

public class TextyController : Controller
{
    public async Task<IActionResult> OProjektu()
    {
        return View();
    }
    
    public async Task<IActionResult> PlatyStatnichZamestnancu()
    {
        return View();
    }
    
    public async Task<IActionResult> Nejvyssi()
    {
        return View();
    }
    
    public async Task<IActionResult> Aktuality()
    {
        return Redirect("/");
    }
    
    public async Task<IActionResult> VypoctyPlatyUredniku()
    {
        return View();
    }

}