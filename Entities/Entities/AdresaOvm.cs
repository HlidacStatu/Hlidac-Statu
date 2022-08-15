using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace HlidacStatu.Entities;

[XmlRoot(ElementName = "AdresaUradu")]
public class AdresaOvm
{
    [XmlElement(ElementName = "AdresniBod")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [XmlElement(ElementName = "UliceNazev")]
    public string UliceNazev { get; set; }

    [XmlElement(ElementName = "CisloDomovni")]
    public int? CisloDomovni { get; set; }

    [XmlElement(ElementName = "ObecNazev")]
    public string ObecNazev { get; set; }

    [XmlElement(ElementName = "ObecKod")]
    public int? ObecKod { get; set; }

    [XmlElement(ElementName = "CastObceNeboKatastralniUzemi")]
    public string CastObceNeboKatastralniUzemi { get; set; }

    [XmlElement(ElementName = "PSC")]
    public int PSC { get; set; }

    [XmlElement(ElementName = "KrajNazev")]
    public string KrajNazev { get; set; }

    [XmlElement(ElementName = "CisloOrientacni")]
    public string CisloOrientacni { get; set; }

    [XmlElement(ElementName = "NestrukturovanaAdresa")]
    public string NestrukturovanaAdresa { get; set; }
}