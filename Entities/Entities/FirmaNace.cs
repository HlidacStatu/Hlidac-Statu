using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Firma_NACE")]
    public partial class FirmaNace
    {
        [Key]
        [Column("ICO")]
        [StringLength(30)]
        public string Ico { get; set; }
        [Key]
        [Column("NACE")]
        [StringLength(50)]
        public string Nace { get; set; }
    }
}
