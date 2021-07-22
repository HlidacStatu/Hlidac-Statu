using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("KOD_PF")]
    public partial class KodPf
    {
        [Key]
        public int Kod { get; set; }
        [Required]
        [StringLength(250)]
        public string PravniForma { get; set; }
    }
}
