using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Entities;

[Table("Firma_DS")]
public class FirmaDs
{
    [Key]
    public string DatovaSchranka { get; set; }
    public string Ico { get; set; }
    public string DsParent { get; set; }
    public string DsSubjName { get; set; }
}