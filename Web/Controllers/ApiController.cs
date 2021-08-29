using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/{action}/{_id?}/{_dataid?}")]
    public partial class ApiController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApiController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public JsonResult Autocomplete(string q)
        {
            var searchCache = StaticData.FulltextSearchForAutocomplete.Get();

            var searchResult = searchCache.Search(q, 5, ac => ac.Priority);

            return Json(searchResult.Select(r => r.Original));
        }

        // GET: ApiV1
        public ActionResult Index()
        {
            return RedirectToAction("Index", "ApiV1");

        }

    }
}