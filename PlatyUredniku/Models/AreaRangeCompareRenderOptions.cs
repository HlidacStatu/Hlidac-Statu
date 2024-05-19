using System.Collections.Generic;

namespace PlatyUredniku.Models;

public class AreaRangeCompareRenderOptions
{
    public AreaRangePlot[] Data { get; set; }
    //public AreaRangePlot Data2 { get; set; }
    public string CssWidth { get; set; } = "100%";
    public string CssHeight { get; set; } = "100%"; //$"{9 / 16 * 100}%'"; //16:9
    public string TextForNoData { get; set; } = string.Empty;
    public string? Title { get; set; } = "Vývoj platů";
    public string? Subtitle { get; set; } = "Vývoj průměrného měsíčního platu po letech";
    public string? Footer { get; set; } = "Zdroj: <a href=\"https://www.ispv.cz\">Informačního systém o průměrném výdělku</a>";

    public int EnableNumSeries { get; set; } = 4;

}
