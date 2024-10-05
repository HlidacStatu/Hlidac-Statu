using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Http;

using Serilog;

namespace HlidacStatu.Web.Framework;

public static class AutocompleteHelper
{
    private static readonly ILogger _logger = Log.ForContext(typeof(AutocompleteHelper));
    
    
    public static List<Autocomplete> CreateInputTagsForJs(IQueryCollection query)
    {
        try
        {
            if (query.TryGetValue("q", out var q))
            {
                if (query.TryGetValue("qtl", out var qtl))
                {
                    var parsedQueries = ParseQueryStringWithOffsets(q, qtl);
                    return HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromParsedQuery(parsedQueries);
                }
                else
                {
                    return HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromQuery(q);
                }
            }
            
            if (query.TryGetValue("qs", out var qs))
            {
                if (query.TryGetValue("qtl", out var qtl))
                {
                    var parsedQueries = ParseQueryStringWithOffsets(qs, qtl);
                    return HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromParsedQuery(parsedQueries);
                }
                else
                {
                    return HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromQuery(qs);
                }
            }
        }
        catch (Exception e)
        { 
            _logger.Error(e, $"During autocomplete usage an error occured. OnParametersSetAsync in Wrapper. query=[{query.ToString()}]");
        }

        return Enumerable.Empty<Autocomplete>().ToList();
    }

    [Obsolete("use HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromParsedQuery")]
    private static List<Autocomplete> _CreateAutocompleteItemsFromparsedQuerieForJs(List<string>? parsedQueries)
    {
        return null;
/*        if (parsedQueries is null)
            return Enumerable.Empty<Autocomplete>().ToList();
        return parsedQueries.AsParallel().Select(CreateAutocompleteItemFromQueryForJs).ToList();
*/
    }

    [Obsolete("use HlidacStatu.Repositories.Searching.Tools.CreateAutocompleteItemsFromParsedQuery")]
    private static Autocomplete _CreateAutocompleteItemFromQueryForJs(string parsedQuery)
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
    
    public static (string queryString, string queryTagLengths) CreateQueryWithOffsets(List<string> input)
    {
        string qs = "";
        string qtl = "";

        for (var index = 0; index < input.Count; index++)
        {
            var item = input[index];

            if (index == input.Count - 1)
            {
                qtl += $"{item.Length}";
                qs += $"{item}";
            }
            else
            {
                qtl += $"{item.Length},";
                qs += $"{item} ";
            }
        }

        return (qs, qtl);
    }

    public static List<string> ParseQueryStringWithOffsets(string queryString, string lengthsString)
    {
        if (string.IsNullOrWhiteSpace(queryString) || string.IsNullOrWhiteSpace(lengthsString))
            return null;

        var offsets = lengthsString.Split(",",
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        List<string> results = new();

        int previousOffset = 0;
        foreach (var offsetText in offsets)
        {
            if (!int.TryParse(offsetText, out int tagLength))
                continue;

            results.Add(queryString.Substring(previousOffset, tagLength));

            // +1 here is a space between tags
            previousOffset += tagLength + 1;
        }

        return results;
    }

}