using System.Threading.Tasks;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public class OsobaController : Controller
    {

        public async Task<ActionResult> Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "Index", $"{osoba.FullName()}");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);

        }

        public async Task<ActionResult> Dotace(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "Dotace", $"{osoba.FullName()} - Dotace");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }
        public async Task<ActionResult> Funkce(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "Funkce", $"{osoba.FullName()} - Veřejné a politické funkce");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }

        public async Task<ActionResult> Sponzoring(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "Sponzoring", $"{osoba.FullName()} - Sponzoring politických stran");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }
        
        public async Task<ActionResult> DalsiDatabaze(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "DalsiDatabaze", $"{osoba.FullName()} - Další databáze");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }

        public async Task<ActionResult> RegistrSmluv(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "RegistrSmluv", $"{osoba.FullName()} - Registr smluv");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }



        public async Task<ActionResult> Vazby(string id, Relation.AktualnostType? aktualnost)
        {
            
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            var popis = "Dceřiné společnosti";
             
            if (aktualnost.HasValue == false)
                aktualnost = Relation.AktualnostType.Nedavny;

            ViewBag.Aktualnost = aktualnost;

            (Osoba osoba, string viewName, string title) model = (osoba, "Vazby", $"{osoba.FullName()} - {popis}");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }



        public async Task<ActionResult> InsolvencniRejstrik(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Osoby");
            }
            
            var osoba = Osoby.GetByNameId.Get(id);
            if (osoba == null)
            {
                return NotFound();
            }
            
            bool isInteresting = await osoba.IsInterestingToShowAsync();
            
            (Osoba osoba, string viewName, string title) model = (osoba, "InsolvencniRejstrik", $"{osoba.FullName()} - Insolvenční rejstřík");
            if (!isInteresting)
            {
                model.viewName = "NoInfo";
                model.title = id;
            }

            return View("_osobaLayout", model);
        }
        

    }
}