using HlidacStatu.Entities.Entities;
using System.Collections.Generic;

namespace HlidacStatu.Repositories.SharedModels;

public class AreaRangeRenderOptions
{
    public ICollection<PuPlat> Platy { get; set; }
    public string CssWidth { get; set; } = "100%";
    public string CssHeight { get; set; } = "100%"; //$"{9 / 16 * 100}%'"; //16:9
    public string TextForNoData { get; set; } = string.Empty;
    public string? Title { get; set; } = "Vývoj platů";
    public string? Subtitle { get; set; } = "Vývoj průměrného měsíčního platu po letech";

}
