using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Entities;

[Table("PU_OranizaceMetadata")]
public class PuOranizaceMetadata
{
    [Key]
    public int Id { get; set; }
    
    public int IdOrganizace { get; set; }
    public int Rok { get; set; }
    public int? ZpusobKomunikace { get; set; }
    public bool? ZduvodneniMimoradnychOdmen { get; set; }
    public DateTime? DatumOdeslaniZadosti { get; set; }
    public DateTime? DatumPrijetiOdpovedi { get; set; }
    public string SkrytaPoznamka { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }
}