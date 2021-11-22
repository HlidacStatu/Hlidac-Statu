using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("FirmyVlastneneStatem")]
    public class FirmaVlastnenaStatem
    {
        [Key]
        [StringLength(30)]
        public string Ico { get; set; }
        public int? Podil { get; set; }
    }
}
