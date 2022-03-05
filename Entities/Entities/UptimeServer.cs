using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities
{
    [Table("UptimeServer")]
    public partial class UptimeServer
    {
        
        [Key]
        [Required]
        [Nest.Keyword]
        public int Id { get; set; }

        [Nest.Number]
        public int Active { get; set; }

        [Nest.Date]
        [Required]
        public DateTime Created { get; set; }
        [Nest.Keyword]
        [Required]
        [StringLength(500)]
        public string PublicUrl { get; set; }
        [StringLength(500)]
        [Nest.Keyword]
        public string RealUrl { get; set; }

        [Nest.Keyword]
        public string AdditionalParams { get; set; }

        [StringLength(300)]
        [Nest.Keyword]
        public string Plugin { get; set; }

        [StringLength(300)]
        [Nest.Keyword] 
        public string Groups { get; set; }


        [Required]
        [StringLength(300)]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }

        [StringLength(30)]
        public string ICO { get; set; }
        [Required]
        public int Priorita { get; set; }

        [Required]
        public int IntervalInSec { get; set; }

        [Nest.Date]
        public DateTime? LastCheck { get; set; }

        [Nest.Number]
        public decimal? LastResponseCode { get; set; }

        [Nest.Number]
        public long? LastResponseTimeInMs { get; set; }

        [Nest.Number]
        public long? LastResponseSize { get; set; }

        [StringLength(30)]
        public string SSLGrade { get; set; }

        [Nest.Date]
        public DateTime? TakenByUptimer { get; set; }

        public int? LastUptimeStatus { get; set; }
        public int? LastAlertedStatus { get; set; }

        public DateTime? LastAlertSent { get; set; }


    }
}
