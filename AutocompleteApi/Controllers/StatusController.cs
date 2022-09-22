using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private CacheService _cacheService;

        public StatusController(CacheService cacheService)
        {
            _cacheService = cacheService;
        }

        [HttpGet]
        public JsonResult OverallStatus()
        {
            using DbEntities db = new DbEntities();

            var firstPolitik = db.Osoba.Where(o => o.Status == 3).FirstOrDefault();


            var resultObj = new
            {
                DbConnection = firstPolitik,
                CacheService = _cacheService.Status,
                UptimeServerRepoServerCount = UptimeServerRepo.AllActiveServers().Length,
            };
            return new JsonResult(resultObj);
        }
    
    }
}