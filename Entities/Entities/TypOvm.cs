using System.Xml.Serialization;

namespace HlidacStatu.Entities;

[XmlRoot(ElementName = "TypSubjektu")]
public class TypOvm
{
    [XmlAttribute(AttributeName = "id")]
    public int Id { get; set; }

    [XmlText]
    public string Text { get; set; }
}