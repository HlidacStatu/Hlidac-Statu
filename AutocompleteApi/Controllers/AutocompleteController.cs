using System.Collections.Generic;
using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using Microsoft.AspNetCore.Mvc;
using MinimalEntities;

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
        public IEnumerable<Autocomplete> Autocomplete(string q, string? category = null, int size = 8)
        {
            var results = _cacheService.FullAutocomplete.Search(q, size * 2, category);
            
            //filter final results by the length of text
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)
                .Take(size)
                .Select(s => s.Document);
        }

        [HttpGet]
        public IEnumerable<SubjectNameCache> Kindex(string q, int size = 10)
        {
            //Web/controllers/KindexController
            var results = _cacheService.Kindex.Search(q, size);
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Name).Length)
                .Select(s => s.Document);
        }
        
        [HttpGet]
        public IEnumerable<Autocomplete> Companies(string q, int size = 10)
        {
            //Web/controllers/apiv1controller
            var results = _cacheService.Company.Search(q, size);
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)
                .Select(s => s.Document);
        }

        [HttpGet]
        public IEnumerable<StatniWebyAutocomplete> UptimeServer(string q, int size = 20)
        {
            //HlidacStatu.Repositories.uptimeserverrepo
            var results = _cacheService.UptimeServer.Search(q, size);
            return results.Select(s => s.Document);
        }
        
        [HttpGet]
        public IEnumerable<AdresyKVolbam> Adresy(string q, int size = 10)
        {
            var results = _cacheService.Adresy.Search(q, size);
            return results.Select(s => s.Document);
        }
        
    }
}