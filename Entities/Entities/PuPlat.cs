using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

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

    decimal? _uvazek = 1;
    public decimal? Uvazek
    {
        get => _uvazek ??= 1;

        set
        {
            if (value == null || value == 0)
                _uvazek = 1;
            else
                _uvazek = value;
        }
    }

    decimal? _pocetMesicu = 12;
    public decimal? PocetMesicu
    {
        get => _pocetMesicu ??= 12;
        set
        {
            if (value == null || value == 0)
                _pocetMesicu = 12;
            else
                _pocetMesicu = value;
        }
    }
    public string NefinancniBonus { get; set; }
    public string PoznamkaPlat { get; set; }
    public string SkrytaPoznamka { get; set; }
    public bool? JeHlavoun { get; set; }
    public int? DisplayOrder { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    public decimal HrubyMesicniPlatVcetneOdmen => ((Plat ?? 0) + (Odmeny ?? 0)) * (1 / Uvazek ?? 1) / (PocetMesicu ?? 12);

    public decimal CelkovyRocniPlatVcetneOdmen => (Plat ?? 0) + (Odmeny ?? 0);
    
    public decimal? PlatMesicne => (Plat ?? 0) / (PocetMesicu ?? 12);
    public decimal? OdmenyMesicne => (Odmeny ?? 0) / (PocetMesicu ?? 12);
    public decimal? OsobniOhodnoceniPerc => ((Plat + Odmeny) == 0 || Plat ==0) ? null : Odmeny / (Plat + Odmeny);
}