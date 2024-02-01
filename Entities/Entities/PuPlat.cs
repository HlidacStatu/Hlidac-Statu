using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Entities;

[Table("PU_Plat")]
public class PuPlat
{
    [Key]
    public int Id { get; set; }
    
    public int IdOrganizace { get; set; }
    public int Rok { get; set; }
    public string NazevPozice { get; set; }
    public decimal? Plat { get; set; }
    public decimal? Odmeny { get; set; }
    public decimal? Uvazek { get; set; }
    public decimal? PocetMesicu { get; set; }
    public string NefinancniBonus { get; set; }
    public string PoznamkaPozice { get; set; }
    public string PoznamkaPlat { get; set; }
    public string SkrytaPoznamka { get; set; }
    public bool? JeHlavoun { get; set; }
    public int? DisplayOrder { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    public decimal HrubyRocniPlat => (Plat ?? 0 + Odmeny ?? 0) * (12 / PocetMesicu ?? 12) * (1 / Uvazek ?? 1);
    
}