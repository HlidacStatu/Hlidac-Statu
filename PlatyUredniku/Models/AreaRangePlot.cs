using System.Collections.Generic;
using System.Linq;

namespace PlatyUredniku.Models;

public class AreaRangePlot
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public int FirstYear => Values.Keys.Min();

    public Dictionary<int, PlotData?> Values { get; set; }

    public string MinMaxTitle { get; set; } = "Rozsah";
    public string ExtraTitle { get; set; } = "Průměr";
    public string MedianTitle { get; set; } = "Medián";


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

    public string DrawExtras()
    {
        var data = Values.Select(kvp => $"[Date.UTC({kvp.Key}, 0, 1), {kvp.Value?.Extra?.ToString("F0") ?? "null"}]");

        return $"[{string.Join(",", data)}]";
    }

    public class PlotData
    {
        public double? Min { get; set; }
        public double? Max { get; set; }
        public double? Median { get; set; }
        public double? Extra { get; set; }

    }
}