using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Devmasters;

namespace HlidacStatu.Entities;

[Table("PU_OrganizaceTags")]
public class PuOrganizaceTag
{
    [Key]
    public int Id { get; set; }
    public int IdOrganizace { get; set; }
    public string Tag { get; set; }
    public string TagNormalized { get; set; }

    // Navigation properties
    [ForeignKey("IdOrganizace")]
    public virtual PuOrganizace Organizace { get; set; }

    public static string NormalizeTag(string tag)
    {
        if(string.IsNullOrWhiteSpace(tag))
            return string.Empty;

        return tag.Trim().RemoveDiacritics().Replace(" ", "-").ToLower();
    }
}