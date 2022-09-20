using HlidacStatu.AutocompleteApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private Caches _cacheService;

        public StatusController(Caches cacheService)
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