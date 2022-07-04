using Microsoft.AspNetCore.Mvc;

namespace HlidacStatuApi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return Content("Taky jsou kone");
        }
    }
}
