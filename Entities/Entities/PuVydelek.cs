using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using Devmasters.Enums;
using HlidacStatu.Entities.Dotace;

namespace HlidacStatu.Entities;

[Table("PU_ISPV_Vydelky")]
public class PuVydelek
{

    [ShowNiceDisplayName(), Groupable()]
    public enum VydelekSektor
    {
        [GroupValue("Mzda")]
        [NiceDisplayName("Soukromý sektor")]
        Soukromy = 1,

        [GroupValue("Plat")]
        [NiceDisplayName("Veřejná správa")]
        StatSamosprava = 2,

        Neuveden = 0
    }
    [NotMapped]
    public VydelekSektor Sektor { 
        get => (VydelekSektor)this.SektorId; 
        set { SektorId = (int)value; }
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Pk { get; set; }
    public int SektorId { get; set; }
    public int Rok { get; set; }
    public string CZ_ISCO { get; set; }
    public int Level { get; set; }
    public string NazevKategorieZamestnani { get; set; }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="format">{0} sektor, {1} kategorie zamestnani</param>
    /// <returns></returns>
    public string FullNazevKategorieZamestnani(string format = "({0}) {1}") {
        var res = string.Format(format, this.Sektor.ToNiceDisplayName(), this.NazevKategorieZamestnani);
        return res;    
    }
    public int PocetZkoumanychOsob { get; set; }
    public decimal Percentil10 { get; set; }
    public decimal Percentil25 { get; set; }
    public decimal Percentil50 { get; set; }
    public decimal Percentil75 { get; set; }
    public decimal Percentil90 { get; set; }
    public decimal Prumer { get; set; }
    public decimal OdmenyPercent { get; set; }
    public decimal PriplatkyPercent { get; set; }
    public decimal NahradyPercent { get; set; }
    public decimal PocetHodinMesicne { get; set; }

}