using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("ZkratkaStrany")]
    public partial class ZkratkaStrany
    {
        [Key]
        [Column("ICO")]
        [StringLength(30)]
        public string Ico { get; set; }
        [StringLength(100)]
        public string KratkyNazev { get; set; }
    }
}
