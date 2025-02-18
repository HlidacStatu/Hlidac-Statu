using Devmasters;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HlidacStatu.Entities;

[Table("PU_Organizace")]
public class PuOrganizace 
{
    public const char PathSplittingChar = '>';

    [Key]
    public int Id { get; set; }

    public string DS { get; set; }
    public string Ico => FirmaDs?.Ico;
    public string Nazev => FirmaDs?.DsSubjName;
    public string Info { get; set; }
    public string HiddenNote { get; set; }
    public string CZISCO { get; set; }

    // Navigation properties
    [ForeignKey("DS")]
    public virtual FirmaDs FirmaDs { get; set; }
    public virtual ICollection<PuOrganizaceTag> Tags { get; set; }
    public virtual ICollection<PuPlat> Platy { get; set; }
    public virtual ICollection<PuPolitikPrijem> PrijmyPolitiku { get; set; }
    public virtual ICollection<PuOrganizaceMetadata> Metadata { get; set; }



    public string GetUrl(bool relative = false)
    {
        var url = $"/Detail/{this.DS}";
        return relative ? url : "https://platyuredniku.hlidacstatu.cz" + url;
    }
}