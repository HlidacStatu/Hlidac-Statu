using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("NespolehlivyPlatceDPH")]
    public partial class NespolehlivyPlatceDPH
    {
        [Key]
        [StringLength(50)]
        public string Ico { get; set; }
        [Column(TypeName = "date")]
        public DateTime? FromDate { get; set; }
        [Column(TypeName = "date")]
        public DateTime? ToDate { get; set; }
    }
}
