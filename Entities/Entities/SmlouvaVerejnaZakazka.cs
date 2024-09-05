using System;
using System.ComponentModel.DataAnnotations;

namespace HlidacStatu.Entities.Entities;

public class SmlouvaVerejnaZakazka
{
    [MaxLength(255)]
    public string VzId { get; set; }
    [MaxLength(255)]
    public string IdSmlouvy { get; set; }
    public double CosineSimilarity { get; set; }
    public DateTime ModifiedDate { get; set; }
}