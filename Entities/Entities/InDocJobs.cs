using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public class InDocJobs
    {
        [Key]
        public long Pk { get; set; }
        [Required]
        public long TablePk { get; set; }
        [Required]
        [StringLength(300)]
        public string JobRaw { get; set; }
        [StringLength(300)]
        public string JobGrouped { get; set; }
        public decimal? SalaryMD { get; set; }
        public decimal? SalaryMdVAT { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? Created { get; set; }
        public string Tags { get; set; }
    }
}