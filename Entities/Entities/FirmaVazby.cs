using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HlidacStatu.Datastructures.Graphs;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("FirmaVazby")]
    [Microsoft.EntityFrameworkCore.Index(nameof(Ico), Name = "IX_FirmaVazby_ICO")]
    [Microsoft.EntityFrameworkCore.Index(nameof(VazbakIco), nameof(Ico), Name = "IX_FirmaVazby_Vazby")]
    [Microsoft.EntityFrameworkCore.Index(nameof(Ico), nameof(VazbakIco), nameof(Pk), Name = "_dta_index_FirmaVazby_9_1701581100__K2_K3_K1_4_5_6_7_8")]
    [Microsoft.EntityFrameworkCore.Index(nameof(VazbakIco), nameof(Pk), nameof(Ico), Name = "_dta_index_FirmaVazby_9_1701581100__K3_K1_K2_4_5_6_7_8")]
    public partial class FirmaVazby
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        [Required]
        [Column("ICO")]
        [StringLength(30)]
        public string Ico { get; set; }
        [Required]
        [Column("VazbakICO")]
        [StringLength(30)]
        public string VazbakIco { get; set; }
        public int? TypVazby { get; set; }
        [StringLength(150)]
        public string PojmenovaniVazby { get; set; }
        [Column("podil", TypeName = "decimal(18, 9)")]
        public decimal? Podil { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DatumOd { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DatumDo { get; set; }
        [StringLength(50)]
        public string Zdroj { get; set; }
        [Column(TypeName = "date")]
        public DateTime LastUpdate { get; set; }
        public int? RucniZapis { get; set; }
        [StringLength(500)]
        public string Poznamka { get; set; }
        
        [NotMapped]
        public Relation.RelationEnum Vazba
        {
            get
            {
                return (Relation.RelationEnum)TypVazby;
            }
            set
            {
                TypVazby = (int)value;
            }
        }
    }
}
