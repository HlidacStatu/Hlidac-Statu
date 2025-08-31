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
    public ActionResult<Autocomplete[]> Main([FromQuery] string q)
    {
        Autocomplete[] results = Array.Empty<Autocomplete>();
        try
        {
            results = AutocompleteService.Search(q).ToArray();
            return results;
        }
        catch (Exception e)
        {
            //TODO: add logging
        }
        return results;
    }

    [HttpGet("dump")]
    public ActionResult<string[]> Dump(string id = "")
    {
        if (this.User.IsInRole("Admin"))
        {
            if (id == "category")
                return AutocompleteCategoryService.DumpAllDocuments();
            else
                return AutocompleteService.DumpAllDocuments();
        }

        return Array.Empty<string>();

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


}