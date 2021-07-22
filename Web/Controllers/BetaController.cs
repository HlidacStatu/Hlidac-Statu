using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace HlidacStatu.Web.Controllers
{
    public class BetaController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Search()
        {
            return View();
        }


        // Used for searching
        public JsonResult Autocomplete(string q)
        {
            var searchCache = StaticData.FulltextSearchForAutocomplete.Get();

            var searchResult = searchCache.Search(q, 5, ac => ac.Priority);

            return Json(searchResult.Select(r => r.Original));
        }

        public ActionResult FiveHundred()
        {
            int result = 0;
            for (int i = 5; i >= 0; i--)
            {
                result = 10 / i;
            }
            
            return Ok($"V pořádku {result}");
        }
    }
}