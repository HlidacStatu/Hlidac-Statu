using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities
{
    [Table("FirmaObor")]
    public class FirmaObor
    {
        [Key]
        [StringLength(30)]
        public string ICO { get; set; } = null!;

        public int OborId { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Modified { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
