using System.Collections.Generic;
using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SearchController : ControllerBase
    {
        private MemoryStoreService _memoryStore;

        public SearchController(MemoryStoreService memoryStore)
        {
            _memoryStore = memoryStore;
        }

        [HttpGet]
        public IEnumerable<Autocomplete> Autocomplete(string q)
        {
            var searchResult = _memoryStore.HlidacFulltextIndex.Search(q, 5, ac => ac.Priority);

            return searchResult.Select(r => r.Original);
        }
        
        [HttpGet]
        public IEnumerable<Autocomplete> SampleComplete(string q)
        {
            var searchResult = _memoryStore.SmallSampleIndex.Search(q, 5, ac => ac.Priority);

            return searchResult.Select(r => r.Original);
        }
    
    }
}