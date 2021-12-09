using Newtonsoft.Json.Linq;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("InDocTablesCheck")]
    public partial class InDocTablesCheck
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Required]
        [StringLength(20)]
        public string SmlouvaID { get; set; }
        [Required]
        [StringLength(90)]
        public string PrilohaHash { get; set; }


        [Required]
        public long Page { get; set; }
        [Required]
        public int TableOnPage { get; set; }
        [Required]
        [StringLength(50)]
        public string Algorithm { get; set; }
        [Required]
        public decimal PrecalculatedScore { get; set; }

        [Required]
        public int Year { get; set; }

        [StringLength(50)]
        public string SubjectCheck { get; set; }

        [StringLength(50)]
        public string AlgorithmCheck { get; set; }


    }
}