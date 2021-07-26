using FullTextSearch;
using HlidacStatu.Entities;
using Nest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.ProfilZadavatelu;
using HlidacStatu.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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