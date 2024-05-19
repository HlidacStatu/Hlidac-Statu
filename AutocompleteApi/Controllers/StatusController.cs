using HlidacStatu.AutocompleteApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private IndexCache _cacheService;

        public StatusController(IndexCache cacheService)
        {
            _cacheService = cacheService;
        }

        [HttpGet]
        public ActionResult<Status> OverallStatus()
        {
            var status = _cacheService.Status();

            if (status is null)
                return StatusCode(418, "Not ready");
            
            return new JsonResult(status);
        }
    }
}