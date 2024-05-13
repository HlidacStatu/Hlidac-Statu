using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using Devmasters.Enums;
using HlidacStatu.Entities.Dotace;

namespace HlidacStatu.Entities;

[Table("PU_ISPV_CZISCO")]
public class PuCZISCO
{
    [Key]
    public string CZ_ISCO { get; set; }
    public int Level { get; set; }
    public string NazevKategorieZamestnani { get; set; }
   

}