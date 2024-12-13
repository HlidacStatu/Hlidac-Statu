using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.DS.Api;
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