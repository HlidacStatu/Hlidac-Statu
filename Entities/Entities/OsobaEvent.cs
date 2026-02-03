using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("OsobaEvent")]
    [Microsoft.EntityFrameworkCore.Index(nameof(OsobaId), Name = "IX_OsobaEvent_Osoba")]
    public partial class OsobaEvent
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        public int OsobaId { get; set; }
        [StringLength(200)]
        public string Title { get; set; }
        public string Note { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DatumOd { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DatumDo { get; set; }
        public int Type { get; set; }
        public int? SubType { get; set; }
        public string AddInfo { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal? AddInfoNum { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        public string Zdroj { get; set; }
        public string Organizace { get; set; }
        public int? Status { get; set; }
        [StringLength(20)]
        public string Ico { get; set; }
        [Column("CEO")]
        public int? Ceo { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
    }
}
