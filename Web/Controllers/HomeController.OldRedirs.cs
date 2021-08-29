using Microsoft.AspNetCore.Mvc;


namespace HlidacStatu.Web.Controllers
{
    public partial class HomeController : Controller
    {

        public ActionResult SubjektVazby(string id)
        {
            return RedirectPermanent("/subjekt/vazby/" + id);
        }
    }
}