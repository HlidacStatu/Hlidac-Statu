using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("qTblsInDoc")]
    public partial class QTblsInDoc
    {
        [Key]
        [Column("pk")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Pk { get; set; }

        [Required]
        [Column("smlouvaId")]
        [StringLength(50)]
        public string SmlouvaId { get; set; }

        [Required]
        [Column("prilohaId")]
        [StringLength(150)]
        public string PrilohaId { get; set; }


        [Required]
        [Column("url")]
        public string Url { get; set; }

        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; } = DateTime.Now;

        [Column("done", TypeName = "datetime")]
        public DateTime? Done { get; set; }

        [Column("started", TypeName = "datetime")]
        public DateTime? Started { get; set; }

        [Column("priority")]
        public int? Priority { get; set; } = 10;

        public HlidacStatu.DS.Api.TablesInDoc.Task ToTablesInDocTask()
        {
            return new DS.Api.TablesInDoc.Task()
            {
                prilohaId = this.PrilohaId,
                smlouvaId = this.SmlouvaId,
                url = this.Url,
            };
        }
    }
}
