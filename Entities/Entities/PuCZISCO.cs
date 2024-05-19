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
    public string Kod { get; set; }
    public int Level { get; set; }
    public string Nazev { get; set; }
    public string Groupname { get; set; }
    public string? KratkyNazev { get; set; }
    public bool MameVydelky { get; set; }


}