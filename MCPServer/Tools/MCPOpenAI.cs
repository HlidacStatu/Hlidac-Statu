﻿using HlidacStatu.Connectors.External.ProfilZadavatelu;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Data.External.StatniPokladna;
using HlidacStatu.Repositories;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using static HlidacStatu.Repositories.SmlouvaRepo.Searching;
using static HlidacStatu.XLib.Search;

namespace HlidacStatu.MCPServer.Tools
{

    //https://platform.openai.com/docs/mcp
    //povinna kompatibilita s MCP pro ChatGPT
    [McpServerToolType]
    public class MCPOpenAI
    {
        static Serilog.ILogger _logger = Serilog.Log.ForContext<MCPDotace>();


        /*
    //https://platform.openai.com/docs/mcp

         search tool
The search tool is used by deep research models (and others) to return a list of possibly relevant search results from the data set exposed by your MCP server.

Arguments:

A single query string.

Returns:

An array of objects with the following properties:

id - a unique ID for the document or search result item
title - a string title for the search result item
text - a relevant snippet of text for the search terms
url - a URL to the document or search result item. Useful for citing specific resources in research.

         */
        [McpServerTool(
            Name = "search",
            Title = "search tool for OpenAI deep search functionality. "),
            Description("return a list of contracts of Czech government for query.")]
        public async static Task<OpenAiResultItem[]> search(string query)
        {
            int maxResults = 50;
            int maxPartSize = 5;
            Dictionary<PartsToSearch, int> parts = new() {
                    { PartsToSearch.Smlouvy ,0 },
                    { PartsToSearch.Dotace ,0 },
                    { PartsToSearch.Firmy ,0 },
                    { PartsToSearch.Osoby ,0 } };

            //PartsToSearch partsLogical = PartsToSearch.Smlouvy | PartsToSearch.Dotace | PartsToSearch.Firmy | PartsToSearch.Osoby;
            PartsToSearch partsLogical = parts.Keys.Aggregate((current, next) => current | next);


            var sres = await XLib.Search
                .GeneralSearchAsync(query, 1, partsLogical, false, null,
                smlouvySize: maxResults,
                firmySize: maxResults,
                osobySize: maxResults,
                dotaceSize: maxResults,
                withHighlighting: true
                );


            List<OpenAiResultItem> res = new List<OpenAiResultItem>();


            while (res.Count < maxResults)
            {
                foreach (PartsToSearch p in Enum.GetValues<PartsToSearch>())
                {
                    if (partsLogical.HasFlag(p))
                    {
                        switch (p)
                        {
                            case PartsToSearch.Smlouvy:
                                if (sres.HasSmlouvy)
                                {
                                    res.AddRange(
                                        sres.Smlouvy.ElasticResults.Hits
                                            .Skip(parts[p])
                                            .Take(maxPartSize)
                                            .Select(x => new OpenAiResultItem
                                            {
                                                id = "smlouva-" + x.Source.Id,
                                                title = x.Source.predmet,
                                                text = string.Join(" ... ",
                                                        x.Highlight
                                                        .Where(m => m.Value?.Count > 0)
                                                        .Select(m => string.Join(" ", m.Value.Distinct()))
                                                        .Select(s => s.Replace("<highl>", "").Replace("</highl>", ""))
                                                        .Distinct()
                                                    ),
                                                url = Smlouva.GetUrl(x.Source.Id, false)
                                            }).ToArray()
                                        );
                                }
                                break;
                            case PartsToSearch.Dotace:
                                res.AddRange(
                                        sres.Dotace.ElasticResults.Hits
                                            .Skip(parts[p])
                                            .Take(maxPartSize)
                                            .Select(x => new OpenAiResultItem
                                            {
                                                id = "dotace-" + x.Source.Id,
                                                title = x.Source.ProjectName ?? x.Source.DisplayProject,
                                                text = string.Join(" ... ",
                                                        x.Highlight
                                                        .Where(m => m.Value?.Count > 0)
                                                        .Select(m => string.Join(" ", m.Value.Distinct()))
                                                        .Select(s => s.Replace("<highl>", "").Replace("</highl>", ""))
                                                        .Distinct()
                                                    ),
                                                url = x.Source.GetUrl(false)
                                            }).ToArray()
                                        );
                                break;
                            case PartsToSearch.Firmy:
                                res.AddRange(
                                    sres.Firmy.Result
                                        .Skip(parts[p])
                                        .Take(maxPartSize)
                                        .Select(x => new OpenAiResultItem
                                        {
                                            id = "firma-" + x.ICO,
                                            title = x.Jmeno,
                                            text = x.InfoFacts().RenderFacts(2, true),
                                            url = x.GetUrl(false)
                                        }).ToArray()
                                    );
                                break;
                            case PartsToSearch.Osoby:
                                res.AddRange(
                                    sres.Osoby.Result
                                            .Skip(parts[p])
                                            .Take(maxPartSize)
                                        .Select(x => new OpenAiResultItem
                                        {
                                            id = "osoba-" + x.NameId,
                                            title = x.FullNameWithYear(),
                                            text = x.InfoFactsAsync().ConfigureAwait(false).GetAwaiter().GetResult().RenderFacts(2, true),
                                            url = x.GetUrl(false)
                                        }).ToArray()
                                    );
                                break;
                            default:
                                break;
                        }
                        if (parts.ContainsKey(p))
                            parts[p] += maxPartSize;
                    }
                    if (res.Count >= maxResults)
                        break;
                }

            }
            
            return res.ToArray();
        }

        [McpServerTool(
            Name = "fetch",
            Title = "search tool for OpenAI deep search functionality. "),
            Description("return a list of contracts of Czech government for query.")]
        public async static Task<OpenAiResultItem> fetch(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Array.Empty<OpenAiResultItem>();
            string[] parts = id.Split('-');
            if (parts.Length < 2)
                return Array.Empty<OpenAiResultItem>();
            string type = parts[0];
            string value = string.Join("-", parts.Skip(1));

            OpenAiResultItem res = null;
            switch (type)
            {
                case "smlouva":
                    var smlouva = await SmlouvaRepo.LoadAsync(value, includePrilohy: true);
                    res = new OpenAiResultItem
                    {
                        id = smlouva.Id,
                        title = smlouva.predmet,
                        text = string.Join(" \n \n", smlouva.Prilohy.Select(m=>m.PlainTextContent)),
                        url = Smlouva.GetUrl(smlouva.Id, false),
                        metadata = ToDictionary(smlouva.ToApiSmlouvaListItem())
                    };
                    break;
                case "dotace":
                    var dotace = await DotaceRepo.GetAsync(value);
                    res = new OpenAiResultItem
                    {
                        id = dotace.Id,
                        title = dotace.ProjectName ?? dotace.DisplayProject,
                        text = dotace.ProjectDescription ?? "",
                        url = dotace.GetUrl(false),
                        metadata = ToDictionary(dotace.ToApiSubsidyDetail())
                    };
                    break;
                case "firma":
                    var firma = Firmy.Get(value);
                    res = new OpenAiResultItem
                    {
                        id = firma.ICO,
                        title = firma.Jmeno,
                        text = firma.InfoFacts().RenderFacts(2, true),
                        url = firma.GetUrl(false)                        
                    };
                    break;
                case "osoba":
                    var osoba = Osoby.GetByNameId.Get(value);
                    res = new OpenAiResultItem
                    {
                        id = osoba.NameId,
                        title = osoba.FullNameWithYear(),
                        text = (await osoba.InfoFactsAsync()).RenderFacts(5, true),
                        url = osoba.GetUrl(false),
                        metadata = ToDictionary(osoba.ToApiOsobaDetail(DateTime.Now.AddYears(-10)))
                    };
                    break;
                default:
                    return Array.Empty<OpenAiResultItem>();
            }
            return res;


        }

        public class OpenAiResultItem
        {
            public string id { get; set; }
            public string title { get; set; }
            public string text { get; set; }
            public string url { get; set; }
            public KeyValuePair<string, string>[]? metadata { get; set; } = null;
        }

        public static KeyValuePair<string, string>[] ToDictionary( object obj)
        {
            if (obj == null)
                return null;

            var json = JsonSerializer.Serialize(obj);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            return dict?
                .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? null))?
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))?
                .ToArray();
        }
    }
}
