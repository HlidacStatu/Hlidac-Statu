using System.Collections.Generic;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.LibCore.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AutocompleteController : ControllerBase
    {
        private readonly IndexCache _cacheService;

        public AutocompleteController(IndexCache cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Vrátí data pro vše z hlídače státu
        /// </summary>
        /// <param name="q">dotaz</param>
        /// <param name="category">Kategorie oddělené mezerami. Seznam kategorií je v CategoryEnum </param>
        /// <returns></returns>
        [HttpGet]
        public IEnumerable<object>? Autocomplete(string q, string? category = null, int size = 8)
        {
            return _cacheService.Search(AutocompleteIndexType.Full, q, size, category);
        }

        [HttpGet]
        public IEnumerable<object>? Kindex(string q, int size = 10)
        {
            return _cacheService.Search(AutocompleteIndexType.KIndex, q, size);
        }
        
        [HttpGet]
        public IEnumerable<object>? Companies(string q, int size = 10)
        {
            return _cacheService.Search(AutocompleteIndexType.Company, q, size);
        }

        [HttpGet]
        public IEnumerable<object>? UptimeServer(string q, int size = 20)
        {
            return _cacheService.Search(AutocompleteIndexType.Uptime, q, size);
        }
        
        [HttpGet]
        public IEnumerable<object>? Adresy(string q, int size = 10)
        {
            return _cacheService.Search(AutocompleteIndexType.Adresy, q, size);
        }
        
    }
}