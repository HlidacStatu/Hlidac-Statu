using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api.Voice2Text
{
    public class Record
    {
        [Key]
        [Column("qId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QId { get; set; }

        [Required]
        [Column("callerId")]
        [StringLength(64)]
        public string CallerId { get; set; }

        [Required]
        [Column("callerTaskId")]
        [StringLength(64)]
        public string CallerTaskId { get; set; }

        [Required]
        [Column("source")]
        public string Source { get; set; }


        [Required]
        [Column("sourceOptions")]
        public string SourceOptions { get; set; }

        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }

        [Column("done", TypeName = "datetime")]
        public DateTime? Done { get; set; }
        [Column("started", TypeName = "datetime")]
        public DateTime Started { get; set; } = DateTime.Now;

        [Column("lastUpdate", TypeName = "datetime")]
        public DateTime? LastUpdate { get; set; }

        [Column("result")]
        public string Result { get; set; }

        [Column("status")]
        public int? Status { get; set; }

        [Column("priority")]
        public int? Priority { get; set; }

    }
}
