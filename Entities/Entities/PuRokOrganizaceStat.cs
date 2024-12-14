using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

public class PuRokOrganizaceStat
{
    public int PocetOslovenych { get; set; }
    public int PocetCoPoslaliPlat { get; set; }
    public Dictionary<int, decimal> PercentilyPlatu { get; set; }
    public Dictionary<int, decimal> PercentilyPlatuHlavounu { get; set; }

}