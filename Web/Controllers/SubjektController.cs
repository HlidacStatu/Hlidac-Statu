using System.Linq;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public partial class SubjektController : Controller
    {

        public ActionResult Index(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Index", $"{firma.Jmeno}");
                return View("_subjektLayout", model);
            }

            return result;
            
        }
        
        public ActionResult Dotace(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Dotace", $"Dotace");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult Rizika(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Rizika", $"Sledovaná rizika");
                return View("_subjektLayout", model);
            }

            return result;
        }


        public ActionResult ObchodySeSponzory(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "ObchodySeSponzory", $"Smlouvy se sponzory politických stran");
                return View("_subjektLayout", model);
            }

            return result;
        }
        public ActionResult DalsiDatabaze(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "DalsiDatabaze", $"Další databáze");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult Sponzoring(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Sponzoring", $"Sponzoring politických stran");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult RegistrSmluv(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "RegistrSmluv", $"Registr smluv");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult VerejneZakazky(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "VerejneZakazky", $"Veřejné zakázky");
                return View("_subjektLayout", model);
            }

            return result;
        }


        public ActionResult Vazby(string id, Relation.AktualnostType? aktualnost)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                var popis = "Dceřinné společnosti";
                if (firma.JsemOVM())
                    popis = "Podřízené organizace";

                if (aktualnost.HasValue == false)
                    aktualnost = Relation.AktualnostType.Nedavny;

                ViewBag.Aktualnost = aktualnost;

                (Firma firma, string viewName, string title) model = (firma, "Vazby", $"{popis}");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult Odberatele(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Odberatele", $"Odběratelé");
                return View("_subjektLayout", model);
            }

            return result;
        }
        
        public ActionResult Dodavatele(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "Dodavatele", $"Dodavatelé");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public ActionResult OrganizacniStruktura(string id, string orgId)
        {
            //ico => id translation!
            if (!StaticData.OrganizacniStrukturyUradu.Get().TryGetValue(id, out var ossu))
            {
                return RedirectToAction("Index");
            }

            D3GraphHierarchy dataHierarchy;

            if (ossu.Count > 1)
            {
                dataHierarchy = ossu.Where(o => o.id == orgId).FirstOrDefault()?.GenerateD3DataHierarchy();
            }
            else
            {
                dataHierarchy = ossu.FirstOrDefault()?.GenerateD3DataHierarchy();
            }

            return dataHierarchy is null ? RedirectToAction("Index") : (ActionResult)View(dataHierarchy);
        }

        public ActionResult DalsiInformace(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "DalsiInformace", $"Informace z registrů");
                return View("_subjektLayout", model);
            }
            return result;

        }


        public ActionResult InsolvencniRejstrik(string id)
        {
            if (TryGetCompany(id, out var firma, out var result))
            {
                (Firma firma, string viewName, string title) model = (firma, "InsolvencniRejstrik", $"Insolvenční rejstřík" );
                return View("_subjektLayout", model);
                //return View((Firma: firma, Data: new List<int>()));
            }

            return result;
        }

        [NonAction()]
        private bool TryGetCompany(string id, out Firma firma, out ActionResult actionResult)
        {
            firma = null;
            
            if (string.IsNullOrWhiteSpace(id))
            {
                actionResult = RedirectToAction("Index", "Home");
                return false;
            }
                
            string ico = Util.ParseTools.NormalizeIco(id);

            firma = Firmy.Get(ico);

            if (!Firma.IsValid(firma))
            {
                if (Util.DataValidators.IsFirmaIcoZahranicni(ico))
                    actionResult = View("Subjekt_zahranicni", new Firma() { ICO = ico, Jmeno = ico });
                else
                {
                    if (!Util.DataValidators.CheckCZICO(ico))
                        actionResult = View("Subjekt_err_spatneICO");
                    else
                        actionResult = View("Subjekt_err_nezname");
                }
                return false;
            }
            if (Util.DataValidators.IsFirmaIcoZahranicni(ico))
            {
                actionResult = View("Subjekt_zahranicni", firma);
                return false;
            }

            actionResult = View("Index",firma);
            return true;
        }

    }
}