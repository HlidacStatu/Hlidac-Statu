using System;
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
    public virtual ICollection<PpPrijem> PrijmyPolitiku { get; set; }
    public virtual ICollection<PuOrganizaceMetadata> Metadata { get; set; }


    public bool HasMetadataForYear(int year, PuOrganizaceMetadata.TypMetadat typMetadat)
    {
        if (Metadata == null || Metadata.Count == 0) 
            throw new Exception($"No metadata found for this organizace DS={DS}. Did you loaded it?");
        
        return Metadata.Any(m => m.Rok == year && m.Typ == typMetadat);
    }



    public string GetUrl(bool relative = false)
    {
        var url = $"/Detail/{this.DS}";
        return relative ? url : "https://platy.hlidacstatu.cz" + url;
    }

    [NotMapped]
    public IEnumerable<PuOrganizaceMetadata> MetadataPlatyUredniku =>
        Metadata.Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyUredniku);
    [NotMapped]
    public IEnumerable<PuOrganizaceMetadata> MetadataPrijmyPolitiku =>
        Metadata.Where(m => m.Typ == PuOrganizaceMetadata.TypMetadat.PlatyPolitiku);



}