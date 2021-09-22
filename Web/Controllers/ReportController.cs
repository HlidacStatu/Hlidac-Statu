using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using System;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Report")]
    public class ReportController : Controller
    {

        [Route("2")]
        public IActionResult SmlouvySChybami()
        {
            ViewBag.Title = "Smlouvy s chybami";
            ViewBag.SubTitle = "Každou smlouvu analyzujeme a hledáme v ní chyby. Zde jsou ty nejhorší smlouvy.";
            return View("2_SmlouvySChybami");
        }

        [Route("3")]
        public IActionResult DraheSmlouvySChybami()
        {
            ViewBag.Title = "Smlouvy s chybami s cenou alespoň 5 mil Kč.";
            ViewBag.SubTitle = "Každou smlouvu analyzujeme a hledáme v ní chyby. Zde jsou ty nejhorší smlouvy na vyšší částky.";
            return View("3_DraheSmlouvySChybami");
        }

        [Route("4")]
        public IActionResult NejdrazsiSmlouvy(int? id)
        {
            ViewBag.Title = "Smlouvy na nejvyšší částky";
            return View("4_NejDrazsiSmlouvy");
        }

        [Route("27")]
        public IActionResult CovidPodpora(int? id)
        {
            ViewBag.Title = "Jaká je slibovaná a jaká je skutečná pomoc státu v dobách koronavirové epidemie?";
            ViewBag.SocialShareText = "Jaká je slibovaná a jaká je skutečná pomoc státu v dobách koronavirové epidemie? Ve spolupráci s Rekonstrukcí státu hlídáme, kolik stát skutečně vyplatil na podpoře podnikatelům a občanům.";
            ViewBag.SubTitle = "";
            ViewBag.SocialShareTitle = "Jaká je skutečná pomoc státu v dobách koronavirové epidemie?";
            ViewBag.SocialImage = $"https://www.hlidacstatu.cz/socialbanner/page?d={DateTime.Now.ToString("d.M.yy")}&v=" + System.Net.WebUtility.UrlEncode(HttpContext.Request.GetDisplayUrl());
            ViewBag.OpenGraphMore = "<meta property=\"og:image:width\" content=\"1920\" />\n"
                    + "<meta property=\"og:image:height\" content=\"1080\" />"
                    + "<meta property=\"og:image:type\" content=\"image/png\" />";
            return View("27_COVIDPodpora");
        }


        [Route("25")]
        [HlidacCache(12 * 60 * 60, "id;obdobi", true)]
        public IActionResult Report25()
        {
            ViewBag.ReportNum = 25;
            ViewBag.Title = "Které politické strany mají největší podíl na řízení státu?";
            ViewBag.SocialShareText = "Jak dlouho zodpovídají jednotlivé politické strany za fungování státu a jednotlivé resorty? Které strany jsou tradiční a které se jimi pomalu stávají?";
            ViewBag.SubTitle = "";
            ViewBag.SocialShareTitle = "Které politické strany mají největší podíl na řízení státu?";
            ViewBag.SocialImage = $"https://www.hlidacstatu.cz/socialbanner/page?d={DateTime.Now.ToString("d.M.yy")}&v=" + System.Net.WebUtility.UrlEncode(HttpContext.Request.GetDisplayUrl());
            ViewBag.OpenGraphMore = "<meta property=\"og:image:width\" content=\"1920\" />\n"
                                    + "<meta property=\"og:image:height\" content=\"1080\" />"
                                    + "<meta property=\"og:image:type\" content=\"image/png\" />";
            return View("25_MinisterstvaStrany");
        }

        [Route("28")]
        public IActionResult Ministerstva(int? id)
        {
            ViewBag.Title = "Které politické strany mají největší podíl na řízení státu?";
            ViewBag.SocialShareText = "Jak dlouho zodpovídají jednotlivé politické strany za fungování státu a jednotlivé resorty? Které strany jsou tradiční a které se jimi pomalu stávají?";
            ViewBag.SubTitle = "";
            ViewBag.SocialShareTitle = "Které politické strany mají největší podíl na řízení státu?";
            ViewBag.SocialImage = $"https://www.hlidacstatu.cz/socialbanner/page?d={DateTime.Now.ToString("d.M.yy")}&v=" + System.Net.WebUtility.UrlEncode(HttpContext.Request.GetDisplayUrl());
            ViewBag.OpenGraphMore = "<meta property=\"og:image:width\" content=\"1920\" />\n"
                    + "<meta property=\"og:image:height\" content=\"1080\" />"
                    + "<meta property=\"og:image:type\" content=\"image/png\" />";
            return View("28_Ministerstva");
        }

        [Route("99")]
        public IActionResult CovidUmrti()
        {
            return View("99_COVID_Umrti");
        }

        [Route("999")]
        public IActionResult CovidNakazeni(int? id)
        {
            return View("999_COVID_Nakazeni");
        }


        [Route("{id:int}")]
        [HlidacCache(12 * 60 * 60, "id", true)]
        public ActionResult OtherReports(int? id)
        {
            if (id.HasValue == false)
                id = 1;

            ViewBag.ReportNum = id;
            switch (id)
            {
                case 1:
                    ViewBag.Title = "Nejčastější smluvní partneři";
                    ViewBag.SubTitle = "Firmy s největším příjmem ze smluv a firmy s největším množstvím uzavřených smluv.";
                    return View("1_PocetSmlouvPerDodavatel");
                case 5:
                    ViewBag.Title = "Smlouvy, u kterých existuje vazba mezi firmami a politiky";
                    ViewBag.SubTitle = "Smlouvy se soukromými ekonomickými subjekty, u kterých existuje přímá a nepřímá vazba na politiky.";
                    return View("5_SmlouvySPolitiky");
                case 6:
                    ViewBag.Title = "Nové smluvní strany";
                    ViewBag.SubTitle = "Firmy a úřady, které se nyní objevili v rejstříku poprvé.";
                    return View("6_NejnovejsiFirmy");
                case 7:
                    ViewBag.Title = "Základní statistiky Registru smluv";
                    return View("7_Statistiky");
                case 9:
                    ViewBag.Title = "Firmy založené blízko data uzavření smlouvy";
                    ViewBag.SubTitle = "Přehled firem založených těsně před uzavřením smlouvy nebo až po uzavření smlouvy.";
                    return View("9_SmlouvySNovymiFirmami");
                case 10:
                    ViewBag.Title = "Smlouvy s utajenými informacemi";
                    return View("10_SmlouvyUtajovane");
                case 11:
                    ViewBag.Title = "Úřady nejvíce skrývající své dodavatele";
                    return View("11_SmlouvyUtajovaneSouhrn");
                case 12:
                    ViewBag.Title = "Úřady nejvíce skrývající hodnotu smluv";
                    return View("12_SmlouvyBezUvedeneCenySouhrn");
                case 13:
                    ViewBag.Title = "Report - Smlouvy s utajenými informacemi";
                    return View("13_SmlouvyBezUvedeneCeny");
                case 14:
                    ViewBag.Title = "Report - Úřady nejvíce obchodující s podezřele založenými firmami";
                    return View("14_UradyObchodujiciSNovymiFirmami");
                case 15:
                    ViewBag.Title = "Report - Úřady nejvíce obchodující s firmami navázanými na politiky";
                    return View("15_UradyObchodujiciSFirmamiCoSVazbouNaPolitiky");
                case 16:
                    ViewBag.Title = "Poslanci - členové Poslanecké sněmovny Parlamentu České republiky po volbách 2017";
                    ViewBag.SubTitle = "Sponzorují noví poslanci politické strany? A mají obchodní vztahy se státem?";
                    return View("16_Poslanci2017");
                case 18:
                    ViewBag.Title = "Hejtmani krajů ČR";
                    ViewBag.SubTitle = "Stav k 1.2.2021";
                    return View("18_Hejtmani2018");
                case 19:
                    ViewBag.Title = "Poradci na úřadu vlády";
                    ViewBag.SubTitle = "Stav k 5.4.2018";
                    return View("19_PoradciVlady");
                case 20:
                    return RedirectPermanent("/Porovnat");
                case 21:
                    ViewBag.Title = "Aktuální složení vlády";
                    ViewBag.SubTitle = "";
                    return View("21_VladyCR");
                case 22:
                    ViewBag.Title = "";
                    ViewBag.SubTitle = "";
                    return View("22_Urady_S_Urady");
                case 26:
                    ViewBag.Title = "V jakých oborech uzavírá stát nejvíce smluv?";
                    ViewBag.SocialShareText = "Stát uzavírá různé smlouvy z různých důvodů. V jakých oborech je jich nejvíce?";
                    ViewBag.SubTitle = "";
                    ViewBag.SocialShareTitle = "V jakých oblastech stát uzavírá nejvíce smluv?";
                    ViewBag.SocialImage = $"https://www.hlidacstatu.cz/socialbanner/page?d={DateTime.Now.ToString("d.M.yy")}&v=" + System.Net.WebUtility.UrlEncode(HttpContext.Request.GetDisplayUrl());
                    ViewBag.OpenGraphMore = "<meta property=\"og:image:width\" content=\"1920\" />\n"
                            + "<meta property=\"og:image:height\" content=\"1080\" />"
                            + "<meta property=\"og:image:type\" content=\"image/png\" />";
                    return View("26_Classification");
                default:
                    return NotFound();
            }
        }
    }
}