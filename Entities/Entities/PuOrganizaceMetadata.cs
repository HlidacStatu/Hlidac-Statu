using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Devmasters.Enums;

namespace HlidacStatu.Entities.Entities;

[Table("PU_OrganizaceMetadata")]
public class PuOrganizaceMetadata
{
    [Key]
    public int Id { get; set; }
    
    public int IdOrganizace { get; set; }
    public int Rok { get; set; }
    public Komunikace? ZpusobKomunikace { get; set; }
    public bool? ZduvodneniMimoradnychOdmen { get; set; }
    public DateTime? DatumOdeslaniZadosti { get; set; }
    public DateTime? DatumPrijetiOdpovedi { get; set; }
    public string SkrytaPoznamka { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    public enum Komunikace
    {
        [NiceDisplayName("datová schránka")] DatovaSchranka = 1,
        [NiceDisplayName("e-mail")] Email = 2
    }
}