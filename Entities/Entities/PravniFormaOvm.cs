using System.Xml.Serialization;

namespace HlidacStatu.Entities;

[XmlRoot(ElementName = "PravniForma")]
public class PravniFormaOvm
{
    [XmlAttribute(AttributeName = "type")]
    public string Id { get; set; }

    [XmlText]
    public string Text { get; set; }
}