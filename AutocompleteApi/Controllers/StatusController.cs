using System.Collections.Generic;
using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private IMemoryStoreService _memoryStore;

        public StatusController(IMemoryStoreService memoryStore)
        {
            _memoryStore = memoryStore;
        }

        [HttpGet]
        public JsonResult OverallStatus()
        {
            var result = new
            {
                _memoryStore.IsDataRenewalRunning,
                _memoryStore.RunningSince,
                _memoryStore.LastDataRenewalStarted,
                _memoryStore.LastException
            };
            
            return new JsonResult(result);

        }
    
    }
}