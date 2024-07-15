using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MinimalEntities;
using ZiggyCreatures.Caching.Fusion;
using PlatyUredniku.Services;

namespace PlatyUredniku.Controllers;

[ApiController]
[Route("autocomplete")]
public class AutocompleteController : ControllerBase
{
    private readonly IFusionCache _cache;
    private readonly AutocompleteCacheService AutocompleteService;

    public AutocompleteController(AutocompleteCacheService autocompleteService)
    {
        AutocompleteService = autocompleteService;
    }

    [HttpGet("main")]
    public ActionResult<List<Autocomplete>> Main([FromQuery] string q)
    {
        try
        {
            var results = AutocompleteService.Search(q).ToList();
            return results;
        }
        catch (Exception e)
        {
            //todo: add logging
        }
        
        return Enumerable.Empty<Autocomplete>().ToList();
    }
}