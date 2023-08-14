using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Mime;
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
        public ActionResult SaveObtezujiciHovor(string jmeno, string datumHovoru, string casHovoru, 
            string cisloVolaneho, string cisloVolajiciho, 
            string volajiciSpolecnost, string volajiciJmeno, string ucel, string teloperator, string kontakt)
        {
            var obtezujiciHovor = new ObtezujiciHovor()
            {
                Jmeno = jmeno,
                DatumHovoru = datumHovoru,
                CasHovoru = casHovoru,
                CisloVolaneho = cisloVolaneho,
                CisloVolajiciho = cisloVolajiciho,
                VolajiciSpolecnost = volajiciSpolecnost,
                VolajiciJmeno = volajiciJmeno,
                Ucel = ucel,
                Teloperator = teloperator,
                Kontakt = kontakt
            };

            var pdf = ObtezujiciHovorPdf.Create(obtezujiciHovor);

            return File(pdf, MediaTypeNames.Application.Pdf, "obtezujiciHovor.pdf");
        }
        
    }
}