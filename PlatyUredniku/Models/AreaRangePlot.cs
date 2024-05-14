using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace PlatyUredniku.Models;

public class AreaRangePlot
{

    
    public AreaRangePlot() { }

    public static AreaRangePlot ToAreaRangePlotWithPrumer(IEnumerable<PuPlat> platy, string dataName,
        string extraTitle = "Průměr ", string minMaxTitle = "Výdělek od-do ")
    {

        var salariesYearly = platy
            .GroupBy(p => p.Rok, p => p)
            .OrderBy(k => k.Key)
            .ToDictionary(g => g.Key, g => g.Select(x => x).ToList());
        if (salariesYearly.Any())
        {
            Dictionary<int, AreaRangePlot.PlotData?> plotData = new();

            for (int year = salariesYearly.Keys.Min(); year <= salariesYearly.Keys.Max(); year++)
            {
                AreaRangePlot.PlotData? dataForYear = new AreaRangePlot.PlotData();

                if (salariesYearly.TryGetValue(year, out var platyForYear))
                {
                    var hrubeMesicniPlaty = platyForYear
                    .Select(p => (double)p.HrubyMesicniPlat)
                    .ToList();
                    dataForYear.Median = hrubeMesicniPlaty?.Median() ?? null;
                    dataForYear.Min = hrubeMesicniPlaty?.Min() ?? null;
                    dataForYear.Max = hrubeMesicniPlaty?.Max() ?? null;

                    var hrubeMesicniPlatyCeo = platyForYear
                    .Where(p => p.JeHlavoun == true)
                    .Select(p => (double)p.HrubyMesicniPlat);
                    if (hrubeMesicniPlatyCeo.Any())
                    {
                        dataForYear.Extra = hrubeMesicniPlatyCeo.Average();
                    }
                }

                plotData.Add(year, dataForYear);
            }

            var chartData = new AreaRangePlot()
            {
                Title = dataName,
                Subtitle = "vývoj průměrného platu po letech",
                ExtraTitle = "Průměr ",
                MinMaxTitle = "Výdělek od-do ",
                Values = plotData
            };
            return chartData;
        }
        else
            return null; 
    }

    public static AreaRangePlot ToAreaRangePlotWithPrumer(IEnumerable<PuVydelek> data, string dataName, string extraTitle= "Průměr ", string minMaxTitle= "Většina od-do ")
    {
        if (data == null || data.Count() == 0)
            return null;

        Dictionary<int, AreaRangePlot.PlotData> d = data
        .Select(m => new
        {
            rok = m.Rok,
            data = new AreaRangePlot.PlotData() { Extra = (double)m.Prumer, Max = (double)m.Percentil90, Min = (double)m.Percentil10, Median = (double)m.Percentil50 }
        })
        .ToDictionary(m => m.rok, v => v.data);

        if (extraTitle?.EndsWith(" ") == false)
            extraTitle = extraTitle+ " ";
        if (minMaxTitle?.EndsWith(" ") == false)
            minMaxTitle = minMaxTitle + " ";

        var res = new AreaRangePlot() { Values = d, ExtraTitle = extraTitle, MinMaxTitle = minMaxTitle, Title = dataName };

        return res;

    }

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