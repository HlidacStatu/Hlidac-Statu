using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;


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

        
        public JsonResult Autocomplete2(string q)
        {
            var searchCache = StaticData.FulltextSearchForAutocomplete.Get();

            IEnumerable<Entities.Autocomplete> searchResult = Newtonsoft.Json.JsonConvert.DeserializeObject<Entities.Autocomplete[]>(_asdf);

            return Json(searchResult);
        }

        static string _asdf = @"[
  {
    ""id"": ""ico:00006947"",
    ""text"": ""Ministerstvo financí"",
    ""imageElement"": ""fas fa-university"",
    ""type"": ""úřad"",
    ""description"": ""Hlavní město Praha"",
    ""priority"": 2
  },
  {
    ""id"": ""ico:28569113"",
    ""text"": ""MINORR FINANCE a.s."",
    ""imageElement"": ""fas fa-industry-alt"",
    ""type"": ""firma"",
    ""description"": ""Moravskoslezský kraj"",
    ""priority"": 0
  },
  {
    ""id"": ""ico:27415414"",
    ""text"": ""MINT Financial Services, s.r.o., v likvidaci"",
    ""imageElement"": ""fas fa-industry-alt"",
    ""type"": ""firma"",
    ""description"": ""Hlavní město Praha"",
    ""priority"": 0
  },
  {
    ""id"": ""ico:48137430"",
    ""text"": ""Ministerstvo financí České republiky Generální ředitelství cel"",
    ""imageElement"": ""fas fa-industry-alt"",
    ""type"": ""firma"",
    ""description"": """",
    ""priority"": 0
  },
  {
    ""id"": ""ico:00001376"",
    ""text"": ""FEDERÁLNÍ MINISTERSTVO FINANCÍ"",
    ""imageElement"": ""fas fa-industry-alt"",
    ""type"": ""firma"",
    ""description"": """",
    ""priority"": 0
  }
]";

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