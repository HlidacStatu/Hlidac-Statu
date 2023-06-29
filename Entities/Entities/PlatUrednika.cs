using System;
using System.ComponentModel.DataAnnotations;

namespace HlidacStatu.Entities.Entities;

public class PlatUrednika
{
    [Key]
    public long Pk { get; set; }
    
    public string Ico { get; set; }

    public string DruhInstituce { get; set; }
    
    public string NazevInstituce { get; set; }
    
    public string Pozice { get; set; }
    
    public string NazevPlatu { get; set; }

    public int Rok { get; set; }
    
    public Decimal Plat { get; set; }
    public Decimal Odmeny { get; set; }
    
    public int PocetMes { get; set; }
    public Decimal Bonus { get; set; }
    public string NefBonus { get; set; }
    
    public bool HeadOfOffice { get; set; }
    public string Link { get; set; }
}