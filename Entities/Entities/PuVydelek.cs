using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using Devmasters.Enums;
using HlidacStatu.Entities.Dotace;

namespace HlidacStatu.Entities;

[Table("PU_ISPV_Vydelky")]
public class PuVydelek
{

    [ShowNiceDisplayName]
    public enum VydelekTyp
    {
        [NiceDisplayName("Mzda - soukromý sektor")]
        Mzda = 1,

        [NiceDisplayName("Plat - stát, samospráva")]
        Plat = 2,

        Neuveden = 0
    }
    public VydelekTyp TypVydelku { get => (VydelekTyp)this.TypVydelku; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Pk { get; set; }
    public int TypVydelkuId { get; set; }
    public int Rok { get; set; }
    public string CZ_ISCO { get; set; }
    public int Level { get; set; }
    public string NazevKategorieZamestnani { get; set; }
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