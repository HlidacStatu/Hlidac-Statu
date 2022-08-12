using System.Xml.Serialization;
using HlidacStatu.Entities;

[XmlRoot(ElementName = "SeznamOvmIndex")]
public class SeznamOvmIndex
{
    [XmlElement(ElementName = "Subjekt")]
    public List<OrganVerejneMoci> Subjekt { get; set; }
}
