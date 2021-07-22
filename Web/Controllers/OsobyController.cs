using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public class OsobyController : Controller
    {
        // GET: Osoby
        public ActionResult Index(string prefix, string q, bool ftx = false)
        {
            if (!string.IsNullOrEmpty(q))
                return RedirectToAction("Hledat",new { q=q });

            return View();
        }

        public ActionResult Hledat(string q, string osobaNamedId)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Redirect("/osoby");

            if (!string.IsNullOrWhiteSpace(osobaNamedId))
            {
                var o = Osoby.GetByNameId.Get(osobaNamedId);
                if (o != null)
                    return Redirect(o.GetUrl(true));
            }

            var res = OsobaRepo.Searching.SimpleSearch(q, 1, 25, OsobaRepo.Searching.OrderResult.Relevance);
            return View(res);
        }

    }
}