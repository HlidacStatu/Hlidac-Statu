using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("MonitoredTasks")]
    public partial class MonitoredTask
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Pk { get; set; }

        [Required]
        [StringLength(200)]
        public string Application { get; set; }

        [Required]
        [StringLength(500)]
        public string Part { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? Started { get; set; } = DateTime.Now;

        [Column(TypeName = "datetime")]
        public DateTime? Finished { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime ItemUpdated { get; set; }

        public decimal? Progress { get; set; }

        public bool Success { get; set; }
        public string Exception { get; set; }

        public string CallingStack { get; set; }
        
    }
}