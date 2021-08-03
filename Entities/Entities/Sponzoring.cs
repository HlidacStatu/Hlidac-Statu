using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Sponzoring")]
    [Index(nameof(IcoDarce), Name = "idx_sponzoring_IcoDarce")]
    [Index(nameof(IcoPrijemce), Name = "idx_sponzoring_IcoPrijemce")]
    [Index(nameof(OsobaIdDarce), Name = "idx_sponzoring_OsobaIdDarce")]
    [Index(nameof(OsobaIdPrijemce), Name = "idx_sponzoring_OsobaIdPrijemce")]
    public partial class Sponzoring
    {
        [Key]
        public int Id { get; set; }
        public int? OsobaIdDarce { get; set; }
        [StringLength(20)]
        public string IcoDarce { get; set; }
        public int? OsobaIdPrijemce { get; set; }
        [StringLength(20)]
        public string IcoPrijemce { get; set; }
        public int Typ { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal? Hodnota { get; set; }
        public string Popis { get; set; }
        [Column(TypeName = "date")]
        public DateTime? DarovanoDne { get; set; }
        public string Zdroj { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Created { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Edited { get; set; }
        [StringLength(150)]
        public string UpdatedBy { get; set; }
    }
}
