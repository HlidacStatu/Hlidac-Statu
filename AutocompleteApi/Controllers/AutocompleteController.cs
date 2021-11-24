using System.Collections.Generic;
using System.Linq;
using HlidacStatu.AutocompleteApi.Services;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.AutocompleteApi.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class AutocompleteController : ControllerBase
    {
        private readonly IMemoryStoreService _memoryStore;

        public AutocompleteController(IMemoryStoreService memoryStore)
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
        public IEnumerable<Autocomplete> TestAutocomplete(string q)
        {
            var searchResult = _memoryStore.SmallSampleIndex.Search(q, 5, ac => ac.Priority);

            return searchResult.Select(r => r.Original);
        }
    }
}