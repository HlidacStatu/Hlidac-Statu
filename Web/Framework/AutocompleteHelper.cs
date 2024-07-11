using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Http;
using MinimalEntities;
using Serilog;
using WasmComponents.Components.Autocomplete;

namespace HlidacStatu.Web.Framework;

public static class AutocompleteHelper
{
    private static readonly ILogger _logger = Log.ForContext(typeof(AutocompleteHelper));
    
    public static List<AutocompleteItem<Autocomplete>> CreateInputTags(IQueryCollection query)
    {
        try
        {
            if (query.TryGetValue("q", out var q))
            {
                if (query.TryGetValue("qtl", out var qtl))
                {
                    var parsedQueries = Helpers.ParseQueryStringWithOffsets(q, qtl);
                    return CreateAutocompleteItemsFromparsedQueries(parsedQueries);
                }
                else
                {
                    var parsedQueries = Helpers.ParseQueryStringWithoutOffsets(q);
                    return CreateAutocompleteItemsFromparsedQueries(parsedQueries);
                }
            }
        }
        catch (Exception e)
        { 
            _logger.Error(e, $"During autocomplete usage an error occured. OnParametersSetAsync in Wrapper. query=[{query.ToString()}]");
        }

        return Enumerable.Empty<AutocompleteItem<Autocomplete>>().ToList();
    }
    
    private static List<AutocompleteItem<Autocomplete>> CreateAutocompleteItemsFromparsedQueries(List<string>? parsedQueries)
    {
        if (parsedQueries is null)
            return Enumerable.Empty<AutocompleteItem<Autocomplete>>().ToList();
        return parsedQueries.AsParallel().Select(CreateAutocompleteItemFromQuery).ToList();
    }
    
    private static AutocompleteItem<Autocomplete> CreateAutocompleteItemFromQuery(string parsedQuery)
    {
        try
        {
            if (parsedQuery.StartsWith("osobaid:", StringComparison.InvariantCultureIgnoreCase))
            {
                var osoba = OsobaRepo.GetByNameId(parsedQuery.Substring(8));
                if (osoba is not null)
                {
                    return new AutocompleteItem<Autocomplete>(new Autocomplete()
                    {
                        Id = parsedQuery,
                        Text = osoba.FullName(),
                        Category = Autocomplete.CategoryEnum.Person
                    });
                }
            }
            else if (parsedQuery.StartsWith("ico:", StringComparison.InvariantCultureIgnoreCase))
            {
                var firma = FirmaRepo.FromIco(parsedQuery.Substring(4).Trim());
                if (firma is not null)
                {
                    Autocomplete.CategoryEnum kategorie = Autocomplete.CategoryEnum.Company;
                    if (firma.TypSubjektu == Firma.TypSubjektuEnum.Obec)
                    {
                        kategorie = Autocomplete.CategoryEnum.City;
                    }
                    else if (firma.Kod_PF > 110 && firma.JsemOVM() && firma.IsInRS == 1)
                    {
                        kategorie = Autocomplete.CategoryEnum.Authority;
                    }
        
                    return new AutocompleteItem<Autocomplete>(new Autocomplete()
                    {
                        Id = parsedQuery,
                        Text = firma.Jmeno,
                        Category = kategorie
                    });
                }
            }
            else if (parsedQuery.StartsWith("oblast:", StringComparison.InvariantCultureIgnoreCase))
            {
                return new AutocompleteItem<Autocomplete>(new Autocomplete()
                {
                    Id = parsedQuery,
                    Text = parsedQuery,
                    Category = Autocomplete.CategoryEnum.Oblast
                });
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, $"During autocomplete usage an error occured. CreateAutocompleteItemFromQuery in Wrapper. Parsed query [{parsedQuery}]");
        }

        return new AutocompleteItem<Autocomplete>(parsedQuery);
    }
    
    
    public static List<Autocomplete> CreateInputTagsForJs(IQueryCollection query)
    {
        try
        {
            if (query.TryGetValue("q", out var q))
            {
                if (query.TryGetValue("qtl", out var qtl))
                {
                    var parsedQueries = Helpers.ParseQueryStringWithOffsets(q, qtl);
                    return CreateAutocompleteItemsFromparsedQuerieForJs(parsedQueries);
                }
                else
                {
                    var parsedQueries = Helpers.ParseQueryStringWithoutOffsets(q);
                    return CreateAutocompleteItemsFromparsedQuerieForJs(parsedQueries);
                }
            }
        }
        catch (Exception e)
        { 
            _logger.Error(e, $"During autocomplete usage an error occured. OnParametersSetAsync in Wrapper. query=[{query.ToString()}]");
        }

        return Enumerable.Empty<Autocomplete>().ToList();
    }
    
    private static List<Autocomplete> CreateAutocompleteItemsFromparsedQuerieForJs(List<string>? parsedQueries)
    {
        if (parsedQueries is null)
            return Enumerable.Empty<Autocomplete>().ToList();
        return parsedQueries.AsParallel().Select(CreateAutocompleteItemFromQueryForJs).ToList();
    } 
    
    private static Autocomplete CreateAutocompleteItemFromQueryForJs(string parsedQuery)
    {
        try
        {
            if (parsedQuery.StartsWith("osobaid:", StringComparison.InvariantCultureIgnoreCase))
            {
                var osoba = OsobaRepo.GetByNameId(parsedQuery.Substring(8));
                if (osoba is not null)
                {
                    return new Autocomplete()
                    {
                        Id = parsedQuery,
                        Text = osoba.FullName(),
                        Category = Autocomplete.CategoryEnum.Person
                    };
                }
            }
            else if (parsedQuery.StartsWith("ico:", StringComparison.InvariantCultureIgnoreCase))
            {
                var firma = FirmaRepo.FromIco(parsedQuery.Substring(4).Trim());
                if (firma is not null)
                {
                    Autocomplete.CategoryEnum kategorie = Autocomplete.CategoryEnum.Company;
                    if (firma.TypSubjektu == Firma.TypSubjektuEnum.Obec)
                    {
                        kategorie = Autocomplete.CategoryEnum.City;
                    }
                    else if (firma.Kod_PF > 110 && firma.JsemOVM() && firma.IsInRS == 1)
                    {
                        kategorie = Autocomplete.CategoryEnum.Authority;
                    }
        
                    return new Autocomplete()
                    {
                        Id = parsedQuery,
                        Text = firma.Jmeno,
                        Category = kategorie
                    };
                }
            }
            else if (parsedQuery.StartsWith("oblast:", StringComparison.InvariantCultureIgnoreCase))
            {
                return new Autocomplete()
                {
                    Id = parsedQuery,
                    Text = parsedQuery,
                    Category = Autocomplete.CategoryEnum.Oblast
                };
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, $"During autocomplete usage an error occured. CreateAutocompleteItemFromQuery in Wrapper. Parsed query [{parsedQuery}]");
        }

        return new Autocomplete()
        {
            Id = parsedQuery,
            Text = parsedQuery
        };
    }
}