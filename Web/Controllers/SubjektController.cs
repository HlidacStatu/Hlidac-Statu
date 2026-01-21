using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Web.Controllers
{
    public class SubjektController : Controller
    {

        public async Task<ActionResult> Index(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Index", $"{firma.Jmeno}");
                return View("_subjektLayout", model);
            }

            return result;

        }

        public async Task<ActionResult> VozovyPark(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "VozovyPark", $"{firma.Jmeno} - Vozový park");
                return View("_subjektLayout", model);
            }

            return result;
        }
        public async Task<ActionResult> Dotace(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Dotace", $"{firma.Jmeno} - Dotace");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> Rizika(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Rizika", $"{firma.Jmeno} - Sledovaná rizika");
                return View("_subjektLayout", model);
            }

            return result;
        }


        public async Task<ActionResult> ObchodySeSponzory(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "ObchodySeSponzory", $"{firma.Jmeno} - Smlouvy se sponzory politických stran");
                return View("_subjektLayout", model);
            }

            return result;
        }
        public async Task<ActionResult> DalsiDatabaze(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "DalsiDatabaze", $"{firma.Jmeno} - Další informace");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> Sponzoring(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Sponzoring", $"{firma.Jmeno} - Sponzoring politických stran");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> RegistrSmluv(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "RegistrSmluv", $"{firma.Jmeno} - Registr smluv");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> VerejneZakazky(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "VerejneZakazky", $"{firma.Jmeno} - Veřejné zakázky");
                return View("_subjektLayout", model);
            }

            return result;
        }


        public async Task<ActionResult> Vazby(string id, Relation.AktualnostType? aktualnost)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                var popis = "Dceřiné společnosti";
                if (firma.JsemOVM())
                    popis = "Podřízené organizace";

                if (aktualnost.HasValue == false)
                    aktualnost = Relation.AktualnostType.Nedavny;

                ViewBag.Aktualnost = aktualnost;

                (Firma firma, string viewName, string title) model = (firma, "Vazby", $"{firma.Jmeno} - {popis}");
                return View("_subjektLayout", model);
            }

            return result;
        }
        public async Task<ActionResult> VazbyUredni(string id, Relation.AktualnostType? aktualnost)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                var popis = "Úřední vazby na společnosti";

                if (aktualnost.HasValue == false)
                    aktualnost = Relation.AktualnostType.Nedavny;

                ViewBag.Aktualnost = aktualnost;

                (Firma firma, string viewName, string title) model = (firma, "VazbyUredni", $"{firma.Jmeno} - {popis}");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> VazbyOsoby(string id, Relation.AktualnostType? aktualnost)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                var popis = "Osoby s vazbou na " + await FirmaCache.GetJmenoAsync(firma.ICO);
            
                if (aktualnost.HasValue == false)
                    aktualnost = Relation.AktualnostType.Nedavny;

                ViewBag.Aktualnost = aktualnost;

                (Firma firma, string viewName, string title) model = (firma, "VazbyOsoby", $"{firma.Jmeno} - {popis}");
                return View("_subjektLayout", model);
            }

            return result;
        }
        public async Task<ActionResult> Odberatele(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Odberatele", $"{firma.Jmeno} - Odběratelé");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> Dodavatele(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "Dodavatele", $"{firma.Jmeno} - Dodavatelé");
                return View("_subjektLayout", model);
            }

            return result;
        }
        
        public async Task<ActionResult> NapojeneOsoby(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "NapojeneOsoby", $"{firma.Jmeno} - Napojené osoby");
                return View("_subjektLayout", model);
            }

            return result;
        }

        public async Task<ActionResult> OrganizacniStruktura(string id, string orgId)
        {
            //ico => id translation!
            var osu = await MaterializedViewsCache.GetOrganizacniStrukturyUraduAsync();
            if (!osu.Urady.TryGetValue(id, out var ossu))
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

            ViewBag.ExporDate = osu.PlatneKDatu;

            return dataHierarchy is null ? RedirectToAction("Index") : (ActionResult)View(dataHierarchy);
        }

        public async Task<ActionResult> DalsiInformace(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "DalsiInformace", $"{firma.Jmeno} - Informace z registrů");
                return View("_subjektLayout", model);
            }
            return result;

        }


        public async Task<ActionResult> InsolvencniRejstrik(string id)
        {
            var (isProperResult, firma, result) = await TryGetCompanyAsync(id);
            if (isProperResult)
            {
                (Firma firma, string viewName, string title) model = (firma, "InsolvencniRejstrik", $"{firma.Jmeno} - Insolvenční rejstřík");
                return View("_subjektLayout", model);
                //return View((Firma: firma, Data: new List<int>()));
            }

            return result;
        }

        [NonAction()]
        private async Task<(bool, Firma? firma, ActionResult actionResult)> TryGetCompanyAsync(string id)
        {
            Firma firma = null;
            ActionResult actionResult = null;
            
            if (string.IsNullOrWhiteSpace(id))
            {
                actionResult = RedirectToAction("Index", "Home");
                return (false, firma, actionResult);
            }

            string ico = Util.ParseTools.NormalizeIco(id);

            firma = await FirmaCache.GetAsync(ico);

            if (firma == null || firma?.Valid == false)
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
                return (false, firma, actionResult);
            }
            if (Util.DataValidators.IsFirmaIcoZahranicni(ico))
            {
                actionResult = View("Subjekt_zahranicni", firma);
                return (false, firma, actionResult);
            }

            actionResult = View("Index", firma);
            return (true, firma, actionResult);
        }

    }
}