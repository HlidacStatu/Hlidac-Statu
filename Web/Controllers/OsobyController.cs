using System.Threading.Tasks;
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
                return RedirectToAction("Hledat", new { q = q });

            return View();
        }

        public async Task<ActionResult> Hledat(string q, string page, string osobaNamedId)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Redirect("/osoby");

            if (!string.IsNullOrWhiteSpace(osobaNamedId))
            {
                var o = Osoby.GetByNameId.Get(osobaNamedId);
                if (o != null)
                    return Redirect(o.GetUrl(true));
            }
            
            int pagenum = string.IsNullOrEmpty(page) ? 1 : int.Parse(page); 

            var res = await OsobaRepo.Searching
                .SimpleSearchAsync(q, pagenum, 25, OsobaRepo.Searching.OrderResult.Relevance);
            return View(res);
        }

    }
}