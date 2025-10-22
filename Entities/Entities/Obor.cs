using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Entities;

[Table("Obor")]
public class Obor
{
    [Key]
    public int Id { get; set; }

    [Column("Obor")]
    public string OborName { get; set; } = null!;
    public string Description { get; set; }
}