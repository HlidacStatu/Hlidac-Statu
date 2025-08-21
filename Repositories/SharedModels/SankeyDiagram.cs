using HlidacStatu.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HlidacStatu.Util;

namespace HlidacStatu.Repositories.SharedModels;

public class SankeyDiagram
{
    public const string Color_primary_light = "#8BB1E5";
    public const string Color_primary = "#0366d6";
    public const string Color_secondary = "#999999";
    public const string Color_secondary_light = "#CCCCCC";
    public const string Color_dark = "#000000";

    public string Color_zjistujeme { get; set; } = Color_dark;
    public string Color_prijem { get; set; } = Color_primary;
    public string Color_nahrady { get; set; } = Color_secondary;

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
        List<object> data = new List<object>();
        
        foreach (var prijemPolitika in PrijmyPolitiku)
        {
            // prijem
            data.Add(new {
                from = "Celkový roční příjem",
                to = prijemPolitika.Organizace.Nazev,
                weight = DrawNakladyPerYear(prijemPolitika.CelkovyRocniPlatVcetneOdmen, nakladyMax),
                color = prijemPolitika.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106 ? Color_zjistujeme : Color_prijem,
                dataLabels = new
                {
                    enabled = true, 
                    format = prijemPolitika.CelkoveRocniNahrady > 1? 
                        $"Celkový roční plat {RenderData.NicePrice(prijemPolitika.CelkovyRocniPlatVcetneOdmen)}" : 
                        RenderData.NicePrice(prijemPolitika.CelkovyRocniPlatVcetneOdmen)
                },
                custom = new {
                    value = prijemPolitika.CelkovyRocniPlatVcetneOdmen,
                    link = $"#{prijemPolitika.Organizace.DS}"
                }
            });

            // nahrady
            if (prijemPolitika.CelkoveRocniNahrady > 1)
            {
                data.Add(new {
                    from = "Celkový roční příjem",
                    to = prijemPolitika.Organizace.Nazev,
                    weight = DrawNakladyPerYear(prijemPolitika.CelkoveRocniNahrady, nakladyMax),
                    color = Color_nahrady,
                    dataLabels = new
                    {
                        enabled = true, 
                        format = $"Celkové roční náhrady {RenderData.NicePrice(prijemPolitika.CelkoveRocniNahrady)}"
                    },
                    custom = new {
                        value = prijemPolitika.CelkoveRocniNahrady,
                        link = $"#{prijemPolitika.Organizace.DS}"
                    }
                });
            }
            
        }

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
    
    
    private double DrawNakladyPerYear(decimal hodnota, decimal nakladyTotal)
    {
        if (hodnota < 1)
            return 1;
        
        double realValue = (double)hodnota;
        double max = (double)nakladyTotal;

        double scaled = realValue/max * 100;
        return scaled;
    }

    private string CheckMissingValue(PpPrijem input)
    {
        return input.Status == PpPrijem.StatusPlatu.Zjistujeme_zadost_106 ? "true" : "false";
    }
}