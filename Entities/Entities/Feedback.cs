using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Feedback")]
    public partial class Feedback
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }

        [Required, StringLength(255)]
        public string ItemId { get; set; }

        [StringLength(255)]
        public string ItemObjectType { get; set; }

        [StringLength(255)]
        public string ItemEvaluatedParameter { get; set; }

        [StringLength(256)]
        public string Reviewer { get; set; }

        [StringLength(15)]
        public string ReviewerIP { get; set; }

        [Required]
        public int Value { get; set; }

        [Required]
        public int ValueMetric { get; set; }

        [NotMapped]
        public ValueMetrics ValueMetricName
        {
            get
            {
                return (ValueMetrics)ValueMetric;
            }
            set { ValueMetric = (int)value; }
        }

        public enum ValueMetrics
        {
            SchoolScale = 1,
            NPS = 2
        }

        public DateTime Created { get; set; } = DateTime.Now;


    }
}