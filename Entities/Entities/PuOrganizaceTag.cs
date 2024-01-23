using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Entities;

[Table("PU_OrganizaceTags")]
public class PuOrganizaceTag
{
    [Key]
    public int Id { get; set; }
    public int IdOrganizace { get; set; }
    public string Tag { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }
}