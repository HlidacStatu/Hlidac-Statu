using HlidacStatu.AutocompleteApi.Services;
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
            return new JsonResult(_cacheService.Status);
        }
    
    }
}