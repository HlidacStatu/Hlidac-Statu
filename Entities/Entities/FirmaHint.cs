using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("FirmaHint")]
    public partial class FirmaHint
    {
        [Key]
        [StringLength(30)]
        public string Ico { get; set; }
        [Column("PocetDni_k_PrvniSmlouve")]
        public int? PocetDniKPrvniSmlouve { get; set; }
    }
}
