using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace HlidacStatu.Repositories.SharedModels;

public class SimplePlot
{
    public string ChartType { get; set; } = "bar";
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    
    
    public string CssWidth { get; set; } = "100%";
    public string CssHeight { get; set; } = "100%"; //$"{9 / 16 * 100}%'"; //16:9
    public string TextForNoData { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string XAxisTitle { get; set; }
    public string YAxisTitle { get; set; }


    public string Koncovka { get; set; } = " Kč";
    public string SeriesName { get; set; }

    
    public List<string> Labels { get; set; } = new();
    public List<double> Data { get; set; } = new();
    public int? YAxisTickInterval { get; set; }


    public string DrawData() => $"[{string.Join(",", Data.Select(x => x.ToString("F1", CultureInfo.InvariantCulture)))}]";
    public string DrawLabels() => $"[{string.Join(",", Labels.Select(l => $"'{l}'"))}]";
    
}