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
    public string PoznamkaHlidace { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    [ShowNiceDisplayName()]
    public enum Komunikace
    {
        [NiceDisplayName("datová schránka")] [Display(Name="datová schránka", Description = "komunikuje datovou schránkou")]DatovaSchranka = 1,
        [NiceDisplayName("e-mail")] [Display(Name="e-mail", Description = "komunikuje emailem")]Email = 2
    }
}