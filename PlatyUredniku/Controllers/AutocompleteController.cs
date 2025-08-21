using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Services;
using HlidacStatu.DS.Api;
using Microsoft.AspNetCore.Authorization;

namespace PlatyUredniku.Controllers;

[ApiController]
[Route("autocomplete")]
public class AutocompleteController : ControllerBase
{
    private readonly AutocompleteCacheService AutocompleteService;
    private readonly AutocompleteCategoryCacheService AutocompleteCategoryService;

    public AutocompleteController(AutocompleteCacheService autocompleteService, AutocompleteCategoryCacheService autocompleteCategoryService)
    {
        AutocompleteService = autocompleteService;
        AutocompleteCategoryService = autocompleteCategoryService;
    }

    [HttpGet("main")]
    public ActionResult<List<Autocomplete>> Main([FromQuery] string q)
    {
        try
        {
            var results = AutocompleteService.Search(q, "instituce oblast osoba").ToList();
            return results;
        }
        catch (Exception e)
        {
            //todo: add logging
        }
        
        return Enumerable.Empty<Autocomplete>().ToList();
    }
    
    [HttpGet("category")]
    public ActionResult<List<Autocomplete>> Category([FromQuery] string q)
    {
        try
        {
            var results = AutocompleteCategoryService.Search(q).ToList();
            return results;
        }
        catch (Exception e)
        {
            //todo: add logging
        }
        
        return Enumerable.Empty<Autocomplete>().ToList();
    }
    
    [Authorize()]
    [HttpGet("admin")]
    public ActionResult<List<Autocomplete>> Admin([FromQuery] string q)
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