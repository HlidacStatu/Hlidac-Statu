using System.Collections.Generic;
using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
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
        public IEnumerable<Autocomplete> Autocomplete(string q, string? category = null)
        {
            var results = _cacheService.FullAutocomplete.Search(q, 10, category);
            
            //filter final results by the length of text
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)
                .Take(5)
                .Select(s => s.Document);
        }

        [HttpGet]
        public IEnumerable<SubjectNameCache> Kindex(string q)
        {
            //Web/controllers/KindexController
            var results = _cacheService.Kindex.Search(q, 10);
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Name).Length)
                .Select(s => s.Document);
        }
        
        [HttpGet]
        public IEnumerable<Autocomplete> Companies(string q)
        {
            //Web/controllers/apiv1controller
            var results = _cacheService.Company.Search(q, 10);
            return results.OrderByDescending(s => s.Score)
                .ThenBy(s => Firma.JmenoBezKoncovky(s.Document.Text).Length)
                .Select(s => s.Document);
        }

        [HttpGet]
        public IEnumerable<StatniWebyAutocomplete> UptimeServer(string q)
        {
            //HlidacStatu.Repositories.uptimeserverrepo
            var results = _cacheService.UptimeServer.Search(q, 20);
            return results.Select(s => s.Document);
        }
        
        [HttpGet]
        public IEnumerable<AdresyKVolbam> Adresy(string q)
        {
            var results = _cacheService.Adresy.Search(q, 10);
            return results.Select(s => s.Document);
        }
        
    }
}