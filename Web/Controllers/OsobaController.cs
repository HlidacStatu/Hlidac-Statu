using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public partial class OsobaController : Controller
    {

        public ActionResult Index(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "Index", $"{osoba.FullName()}");
                return View("_osobaLayout", model);
            }

            return result;

        }

        public ActionResult Dotace(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "Dotace", $"{osoba.FullName()} - Dotace");
                return View("_osobaLayout", model);
            }

            return result;
        }
        public ActionResult Funkce(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "Funkce", $"{osoba.FullName()} - Veřejné a politické funkce");
                return View("_osobaLayout", model);
            }

            return result;
        }

        public ActionResult Sponzoring(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "Sponzoring", $"{osoba.FullName()} - Sponzoring politických stran");
                return View("_osobaLayout", model);
            }

            return result;
        }
        public ActionResult DalsiDatabaze(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "DalsiDatabaze", $"{osoba.FullName()} - Další databáze");
                return View("_osobaLayout", model);
            }

            return result;
        }

        public ActionResult RegistrSmluv(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "RegistrSmluv", $"{osoba.FullName()} - Registr smluv");
                return View("_osobaLayout", model);
            }

            return result;
        }



        public ActionResult Vazby(string id, Relation.AktualnostType? aktualnost)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                var popis = "Dceřinné společnosti";
                //if (firma.JsemOVM())
                //    popis = "Podřízené organizace";

                if (aktualnost.HasValue == false)
                    aktualnost = Relation.AktualnostType.Nedavny;

                ViewBag.Aktualnost = aktualnost;

                (Osoba osoba, string viewName, string title) model = (osoba, "Vazby", $"{osoba.FullName()} - {popis}");
                return View("_osobaLayout", model);
            }

            return result;
        }



        public ActionResult InsolvencniRejstrik(string id)
        {
            if (TryGetOsoba(id, out var osoba, out var result))
            {
                (Osoba osoba, string viewName, string title) model = (osoba, "InsolvencniRejstrik", $"{osoba.FullName()} - Insolvenční rejstřík");
                return View("_osobaLayout", model);
                //return View((Firma: firma, Data: new List<int>()));
            }

            return result;
        }

        [NonAction()]
        private bool TryGetOsoba(string id, out Osoba osoba, out ActionResult actionResult)
        {
            osoba = null;

            if (string.IsNullOrWhiteSpace(id))
            {
                actionResult = RedirectToAction("Index", "Osoby");
                return false;
            }


            osoba = Osoby.GetByNameId.Get(id);

            if (osoba == null)
            {
                actionResult = NotFound();
                return false;
            }


            actionResult = View("Index", osoba);
            return true;
        }

    }
}