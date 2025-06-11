using System;
using HlidacStatu.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace HlidacStatu.Repositories.SharedModels;

public class SankeyDiagram
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public ICollection<PpPrijem> PrijmyPolitiku { get; set; }
    
    public string CssWidth { get; set; } = "100%";
    public string CssHeight { get; set; } = "100%"; //$"{9 / 16 * 100}%'"; //16:9
    public string LinkColor { get; set; } = "#999999";

    public string DrawData()
    {
        var nakladyMax = PrijmyPolitiku.Max(p => p.CelkoveRocniNakladyNaPolitika);
        var data = PrijmyPolitiku.Select(prijem => 
            new {
                from = "Celkový příjem",
                to = prijem.Organizace.Nazev,
                weight = DrawNakladyPerYear(prijem, nakladyMax),
                color = prijem.Status == PpPrijem.StatusPlatu.Zjistujeme ? "#000000" : "#999999",
                custom = new {
                    value = prijem.CelkoveRocniNakladyNaPolitika,
                    link = $"#{prijem.Organizace.DS}"
                }
            });

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase // matches JS expectations (e.g. from, to, weight)
        };

        return JsonSerializer.Serialize(data, options);
    }
    
    public string DrawNodes()
    {
        var total = PrijmyPolitiku.Sum(p => p.CelkoveRocniNakladyNaPolitika);

        var nodes = new List<object>
        {
            new
            {
                id = "Celkový příjem",
                custom = new
                {
                    value = total
                }
            }
        };

        nodes.AddRange(PrijmyPolitiku.Select(p => new
        {
            id = p.Organizace.Nazev,
            custom = new
            {
                value = p.CelkoveRocniNakladyNaPolitika,
                link = $"#{p.Organizace.DS}"
            }
        }));

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(nodes, options);
    }
    
    
    private double DrawNakladyPerYear(PpPrijem input, decimal nakladyTotal)
    {
        if (input.Status == PpPrijem.StatusPlatu.Zjistujeme)
        {
            return 1; // fake-small value to make the line appear
        }

        double realValue = (double)input.CelkoveRocniNakladyNaPolitika;
        double max = (double)nakladyTotal;

        double scaled = realValue/max * 100;
        return scaled;
    }

    private string CheckMissingValue(PpPrijem input)
    {
        return input.Status == PpPrijem.StatusPlatu.Zjistujeme ? "true" : "false";
    }
}