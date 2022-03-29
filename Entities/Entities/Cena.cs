using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Devmasters.Enums;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Ceny")]
    public partial class Cena
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }
        [Required]
        public long JobPk { get; set; }

        [Required]
        public string SmlouvaId { get; set; }

        [Required]
        [StringLength(50)]
        public string IcoOdberatele { get; set; }
        [Required]
        [StringLength(50)]
        public string IcoDodavatele { get; set; }

        [Required]
        public long TablePk { get; set; }

        [Required]
        [StringLength(300)]
        public string Polozka { get; set; }
        public string Tags { get; set; }

        [Required]
        public int Unit { get; set; }
        [Required]
        [StringLength(50)]
        public string UnitText { get; set; }

        // cenu bez DPH nemáme vždy a nejsme ji schopni 100% správně spočítat z ceny s DPH,
        // protože když máme cenu s DPH, tak neevidujeme sazbu DPH
        public decimal? PricePerUnit { get; set; }
        public decimal PricePerUnitVat { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        [StringLength(50)]
        public string AnalyzaName { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? Created { get; set; }
        

    }
}