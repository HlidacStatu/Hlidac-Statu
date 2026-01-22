using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.LibCore.Filters;
using HlidacStatu.RegistrVozidel.Models;
using HlidacStatu.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    public class VozidlaController : Controller
    {

        // GET: Ucty

        public ActionResult Index()
        {
            return View();
        }


        public ActionResult VIN(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            id = id.ToUpper().Trim();   
            using var db = new dbCtx();
            var vv = db.VypisVozidel
                    .AsNoTracking()
                    .Select(m=>m)
                    .Where(v => v.Vin == id)
                    .FirstOrDefault();

            return View(vv);
        }

    }
}