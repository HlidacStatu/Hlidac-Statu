using System;
using HlidacStatu.Entities;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using HlidacStatu.Util;

namespace HlidacStatu.Repositories.SharedModels;

public class SankeyDiagram
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? Description { get; set; }
    public ICollection<PpPrijem> PrijmyPolitiku { get; set; }
    public IEnumerable<string> OrgBezUvedeniPlatu { get; set; }
    
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
                from = "Celkové roční náklady na politika",
                to = prijemPolitika.Organizace.Nazev,
                weight = DrawNakladyPerYear(prijemPolitika.CelkovyRocniPlatVcetneOdmen, nakladyMax),
                color = prijemPolitika.Status == PpPrijem.StatusPlatu.Zjistujeme ? "#000000" : "#999999",
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
                    from = "Celkové roční náklady na politika",
                    to = prijemPolitika.Organizace.Nazev,
                    weight = DrawNakladyPerYear(prijemPolitika.CelkoveRocniNahrady, nakladyMax),
                    color = "#123456",
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
        
        if (OrgBezUvedeniPlatu?.Count() > 0)
        {
            data.AddRange(
                    OrgBezUvedeniPlatu.Select(f=>
                        new
                        {
                            from = "Celkové roční náklady na politika",
                            to = Firmy.GetJmeno(f),
                            weight = DrawNakladyPerYear(1, nakladyMax),
                            color = "#000000",
                            custom = new
                            {
                                value = 0m,
                                link = $"#{f}"
                            }
                        }
                        )
                );
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
        double realValue = (double)hodnota;
        double max = (double)nakladyTotal;

        double scaled = realValue/max * 100;
        return scaled;
    }

    private string CheckMissingValue(PpPrijem input)
    {
        return input.Status == PpPrijem.StatusPlatu.Zjistujeme ? "true" : "false";
    }
}