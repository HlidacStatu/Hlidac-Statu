using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using HlidacStatu.Web.Models;
using HlidacStatu.Web.Pdfs;
using Microsoft.AspNetCore.Http;


namespace HlidacStatu.Web.Controllers
{
    public class PodaniController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        
        public PodaniController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult SaveObtezujiciHovor(string jmeno, string datum, string volany, string volajici, 
            string spolecnost, string ucel, string teloperator, string kontakt)
        {
            var obtezujiciHovor = new ObtezujiciHovor()
            {
                Jmeno = jmeno,
                Datum = datum,
                Volany = volany,
                Volajici = volajici,
                Spolecnost = spolecnost,
                Ucel = ucel,
                Teloperator = teloperator,
                Kontakt = kontakt
            };

            var pdf = ObtezujiciHovorPdf.Create(obtezujiciHovor);

            return Json(new { success = true });
        }
        
    }
}