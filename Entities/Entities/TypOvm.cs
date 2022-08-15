using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Serialization;

namespace HlidacStatu.Entities;

[XmlRoot(ElementName = "TypSubjektu")]
public class TypOvm
{
    [XmlAttribute(AttributeName = "id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }

    [XmlText]
    public string Text { get; set; }
}