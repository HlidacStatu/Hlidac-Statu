using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities;

[Table("PU_PolitikPrijem")]
public class PpPrijem
{
    /// <summary>
    /// Represents the status of a request or process in the system.
    /// </summary>
    /// <remarks>The <see cref="StatusPlatu"/> enumeration defines various states that a request or process
    /// can be in,  ranging from initial submission to confirmation by different entities. Each value corresponds to a 
    /// specific stage in the workflow.</remarks>
    [Devmasters.Enums.ShowNiceDisplayName]
    public enum StatusPlatu
    {
        [Devmasters.Enums.NiceDisplayName("Příjem od politika, který jsme nepřijali jako správný")]
        Prijem_od_politika_neodsouhlaseny = -2,
        [Devmasters.Enums.NiceDisplayName("Příjem od politika, který jsme zatím nezkontrolovali")]
        Prijem_od_politika_nezkontrolovan = -1,
        [Devmasters.Enums.NiceDisplayName("Požádali jsme organizaci o příjem politika, informaci neposkytla")]
        Zjistujeme_zadost_106 = 0,
        [Devmasters.Enums.NiceDisplayName("Příjem nám poskytla organizace.")]
        PotvrzenyPlat_od_organizace = 1,
        [Devmasters.Enums.NiceDisplayName("Příjem nám zaslal politik.")]
        PotvrzenyPlat_od_politika = 2,
        [Devmasters.Enums.NiceDisplayName("Na tento příjem jsme se organizace ani politika neptali")]
        Prijem_nezjistovali_jsme = 3,
        [Devmasters.Enums.NiceDisplayName("Příjem z jiného zdroje.")]
        PotvrzenyPlat_jiny_zdroj = 5,

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

    public string ZdrojInformace { get; set; }
    public StatusPlatu Status { get; set; } = 0;
    
    public string CreatedBy { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime? DateCreated { get; set; } = DateTime.Now;
    public DateTime? DateModified { get; set; } = DateTime.Now;

    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    //todo: tohle je potřeba projít a případně zakomponovat náhrady...
    public decimal CelkoveRocniNakladyNaPolitika => CelkovyRocniPlatVcetneOdmen + CelkoveRocniNahrady;
    public decimal CelkovyRocniPlatVcetneOdmen => (Plat ?? 0) + (Odmeny ?? 0) + (Prispevky ?? 0) 
                                                  + (NahradaReprezentace ?? 0) + (NahradaCestovni ?? 0) + (NahradaUbytovani ?? 0);
    public decimal CelkoveRocniNahrady => (NahradaKancelar ?? 0) + (NahradaAdministrativa ?? 0) + (NahradaAsistent ?? 0) + (NahradaTelefon ?? 0);
    public decimal PrumernyMesicniPrijemVcetneOdmen => (PocetMesicu==0) ? 0 : CelkovyRocniPlatVcetneOdmen / (PocetMesicu ?? 12);
    public decimal? PlatMesicne => (PocetMesicu == 0) ? 0 : ((Plat ?? 0) / (PocetMesicu ?? 12));
    public decimal? OdmenyMesicne => (PocetMesicu == 0) ? 0 : ((Odmeny ?? 0) / (PocetMesicu ?? 12));
    public decimal? OsobniOhodnoceniPerc => ((Plat + Odmeny) == 0 || Plat == 0) ? null : Odmeny / (Plat + Odmeny);
}