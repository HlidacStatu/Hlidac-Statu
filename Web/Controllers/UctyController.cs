using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public class UctyController : Controller
    {

        // GET: Ucty

        [HlidacCache(60 * 60 * 6, "embed", false)]
        public ActionResult Index()
        {
            return RedirectPermanent($"/data/Index/transparentni-ucty");
        }


        [HlidacCache(60 * 60 * 6, "embed", false)]
        public ActionResult Prezidenti()
        {
            return RedirectPermanent($"/data/Hledat/transparentni-ucty?Q=TypSubjektu%3A%22{System.Net.WebUtility.UrlEncode("Prezidentský kandidát")}%22");


        }

        public ActionResult Transakce(string id)
        {
            return RedirectPermanent("/data/Detail/transparentni-ucty-transakce/" + id?.ToLower());

        }

        public ActionResult SubjektyTypu(string id)
        {
            return RedirectPermanent($"/data/Hledat/transparentni-ucty?Q=TypSubjektu%3A%22{System.Net.WebUtility.UrlEncode(id)}%22");

        }

        public ActionResult Ucty(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectPermanent($"/data/Index/transparentni-ucty");

            if (id.Contains("/"))
                id = id.Replace("/", "-");
            return RedirectPermanent($"/data/Hledat/transparentni-ucty?q=Subjekt%3A%22{System.Net.WebUtility.UrlEncode(id)}%22");
        }

        public ActionResult Ucet(string id, int from = 0)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectPermanent($"/data/Index/transparentni-ucty");

            if (id.Contains("/"))
                id = id.Replace("/", "-");
            return RedirectPermanent($"/data/Detail/transparentni-ucty/" + id);
        }



        public ActionResult Hledat(string id, string q, string osobaid, string ico)
        {

            string query = q;
            if (!string.IsNullOrEmpty(id))
                query = $"({query}) AND (CisloUctu:{id.Replace("/", "-")})";


            return RedirectPermanent($"/data/Hledat/transparentni-ucty-transakce?q={System.Net.WebUtility.UrlEncode(query)}");

        }

    }
}