using System.Xml.Serialization;

namespace HlidacStatu.Entities;

public class OrganVerejneMoci_KategorieOvm
{
    public string IdDS { get; set; }
    public string KategorieOvm { get; set; }
}

[XmlRoot(ElementName = "Subjekt")]
public class OrganVerejneMoci
{
    public int Id { get; set; }

    [XmlElement(ElementName = "Zkratka")]
    public string Zkratka { get; set; }

    [XmlElement(ElementName = "ICO")]
    public string ICO { get; set; }

    [XmlElement(ElementName = "Nazev")]
    public string Nazev { get; set; }

    [XmlElement(ElementName = "AdresaUradu")]
    public AdresaOvm AdresaOvm { get; set; }
    public int AdresaOvmId { get; set; }

    [XmlElement(ElementName = "TypSubjektu")]
    public TypOvm TypOvm { get; set; }
    public int TypOvmId { get; set; }

    [XmlElement(ElementName = "PravniForma", IsNullable = true)]
    public PravniFormaOvm PravniFormaOvm { get; set; }
    public string PravniFormaOvmId { get; set; }

    [XmlElement(ElementName = "PrimarniOvm")]
    public string PrimarniOvm { get; set; }

    [XmlElement(ElementName = "IdDS")]
    public string IdDS { get; set; }

    [XmlElement(ElementName = "TypDS")]
    public string TypDS { get; set; }

    [XmlElement(ElementName = "StavDS")]
    public int StavDS { get; set; }

    [XmlElement(ElementName = "StavSubjektu")]
    public int StavSubjektu { get; set; }

    [XmlElement(ElementName = "DetailSubjektu")]
    public string DetailSubjektu { get; set; }

    [XmlElement(ElementName = "IdentifikatorOvm")]
    public string IdentifikatorOvm { get; set; }

    [XmlElement(ElementName = "KategorieOvm")]
    public string KategorieOvm { get; set; }
}