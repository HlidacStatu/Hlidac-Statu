using HlidacStatu.Entities;
using System.Collections.Generic;
using System.Linq;

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
        // [
        //   ['Total', 'Alice', 1000, color:'#999999'],
        //   ['Total', 'Bob', 2000, color:'#999999'],
        //   ['Total', 'Charlie', 1500, color:'#999999'],
        //   ['Total', 'Charlie2', 1500, color:'#999999'],
        //   ['Total', 'Charlie3', 100, color:'#999999']
        // ]
        var data = PrijmyPolitiku.Select(p => $"['Celkový příjem','{p.Organizace.Nazev}',{DrawNakladyPerYear(p)},'{LinkColor}']");

        return $"[{string.Join(",", data)}]";
    }

    public string DrawNodes()
    {
        // [
        // { id: 'Alice', custom: { link: '#alice' } },
        // { id: 'Bob', custom: { link: '#bob' } },
        // { id: 'Charlie', custom: { link: '#charlie' } },
        // { id: 'Charlie2', custom: { link: '#charlie2' } },
        // { id: 'Charlie3', custom: { link: '#charlie3' } }
        // ]
        var data = PrijmyPolitiku.Select(p => $"{{id:'{p.Organizace.Nazev}',custom:{{link: '#{p.Organizace.DS}'}} }}");

        return $"[{string.Join(",", data)}]";
    }

    private string DrawNakladyPerYear(PpPrijem input)
    {
        if (input.Status == PpPrijem.StatusPlatu.Zjistujeme) // neposlali plat
        {
            return "0.00001";
        }
        
        return input.CeloveRocniNakladyNaPolitika.ToString("F0");
        
    }

    private string CheckMissingValue(PpPrijem input)
    {
        return input.Status == PpPrijem.StatusPlatu.Zjistujeme ? "true" : "false";
    }
}