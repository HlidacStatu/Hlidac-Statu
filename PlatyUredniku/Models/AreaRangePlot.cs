using HlidacStatu.Entities.Entities;
using System.Collections.Generic;
using System.Linq;

namespace PlatyUredniku.Models;

public class AreaRangeRenderOptions
{
    public ICollection<PuPlat> Platy { get; set; }
    public string CssWidth { get; set; } = "100%";
    public string CssHeight { get; set; } = "100%"; //$"{9 / 16 * 100}%'"; //16:9
    public string TextForNoData { get; set; } = string.Empty;
    public string? Title { get; set; } = "Vývoj platů";
    public string? Subtitle { get; set; } = "Vývoj průměrného měsíčního platu po letech";

}

public class AreaRangePlot
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public int FirstYear => Values.Keys.Min();

    public Dictionary<int, PlotData?> Values { get; set; }



    public string DrawMinMaxes()
    {
        var data = Values.Select(kvp => $"[Date.UTC({kvp.Key}, 0, 1), {kvp.Value?.Min?.ToString("F0") ?? "null"},{kvp.Value?.Max?.ToString("F0") ?? "null"}]");

        return $"[{string.Join(",", data)}]";
    }

    public string DrawMedians()
    {
        var data = Values.Select(kvp => $"[Date.UTC({kvp.Key}, 0, 1), {kvp.Value?.Median?.ToString("F0") ?? "null"}]");

        return $"[{string.Join(",", data)}]";
    }

    public string DrawCeoSalaries()
    {
        var data = Values.Select(kvp => $"[Date.UTC({kvp.Key}, 0, 1), {kvp.Value?.Ceo?.ToString("F0") ?? "null"}]");

        return $"[{string.Join(",", data)}]";
    }

    public class PlotData
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Median { get; set; }
        public double? Ceo { get; set; }

    }
}