using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

[Table("PU_PolitikPrijem")]
public class PpPrijem
{
    public enum StatusPlatu
    {
        Zjistujeme = 0,
        Potvrzen = 1,
    }

    [Key]
    public int Id { get; set; }

    public int IdOrganizace { get; set; }
    public int Rok { get; set; }
    public string Nameid { get; set; }
    public string NazevFunkce { get; set; }
    public decimal? Plat { get; set; }
    public decimal? Odmeny { get; set; }

    decimal? _pocetMesicu = 12;
    public decimal? PocetMesicu
    {
        get => _pocetMesicu ??= 12;
        set => _pocetMesicu = value == null || value == 0 ? 12 : value;
    }

    public string NefinancniBonus { get; set; }
    public string PoznamkaPlat { get; set; }
    public string SkrytaPoznamka { get; set; }
    public int? Uvolneny { get; set; }
    public int? DisplayOrder { get; set; }

    public decimal? NahradaReprezentace { get; set; }
    public decimal? NahradaCestovni { get; set; }
    public decimal? NahradaKancelar { get; set; }
    public decimal? NahradaUbytovani { get; set; }
    public decimal? NahradaAdministrativa { get; set; }
    public decimal? NahradaAsistent { get; set; }
    public decimal? NahradaTelefon { get; set; }
    public decimal? Prispevky { get; set; }

    public StatusPlatu Status { get; set; } = 0;

    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    //todo: tohle je potřeba projít a případně zakomponovat náhrady...
    public decimal CeloveRocniNakladyNaPolitika => CelkovyRocniPlatVcetneOdmen + CelkoveRocniNahrady;
    public decimal CelkovyRocniPlatVcetneOdmen => (Plat ?? 0) + (Odmeny ?? 0) + (Prispevky ?? 0) 
                                                  + (NahradaReprezentace ?? 0) + (NahradaCestovni ?? 0) + (NahradaUbytovani ?? 0);
    public decimal CelkoveRocniNahrady => (NahradaKancelar ?? 0) + (NahradaAdministrativa ?? 0) + (NahradaAsistent ?? 0) + (NahradaTelefon ?? 0);
    public decimal HrubyMesicniPlatVcetneOdmen => (PocetMesicu==0) ? 0 : CelkovyRocniPlatVcetneOdmen / (PocetMesicu ?? 12);
    public decimal? PlatMesicne => (PocetMesicu == 0) ? 0 : ((Plat ?? 0) / (PocetMesicu ?? 12));
    public decimal? OdmenyMesicne => (PocetMesicu == 0) ? 0 : ((Odmeny ?? 0) / (PocetMesicu ?? 12));
    public decimal? OsobniOhodnoceniPerc => ((Plat + Odmeny) == 0 || Plat == 0) ? null : Odmeny / (Plat + Odmeny);
}