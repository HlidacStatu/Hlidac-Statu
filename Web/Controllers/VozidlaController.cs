using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public class VozidlaController : Controller
    {

        // GET: Ucty

        [HlidacOutputCache(60 * 60 * 6, "embed", false)]
        public ActionResult Index()
        {
            return View();
        }


        
        public ActionResult VIN(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");
            return View((object)id);
        }

    }
}