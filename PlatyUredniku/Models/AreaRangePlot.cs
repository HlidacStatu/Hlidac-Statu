using System.Linq;

namespace PlatyUredniku.Models;

public class AreaRangePlot
{
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public int FirstYear { get; set; }
    public double[][] MinMaxes { get; set; }
    public double[] Medians { get; set; }
    
    public double[] CeoSalaries { get; set; }

    public string DrawMinMaxes()
    {
        var mnms = MinMaxes.Select(m => $"[{m[0]:F0},{m[1]:F0}]");

        return $"[{string.Join(",", mnms)}]";
    }
    
    public string DrawMedians()
    {
        var meds = Medians.Select(m => $"[{m:F0}]");

        return $"[{string.Join(",", meds)}]";
    }
    
    public string DrawCeoSalaries()
    {
        var salaries = CeoSalaries.Select(m => $"[{m:F0}]");

        return $"[{string.Join(",", salaries)}]";
    }
}