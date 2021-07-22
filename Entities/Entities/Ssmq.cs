using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("SSMQ")]
    [Microsoft.EntityFrameworkCore.Index(nameof(Created), Name = "IX_created_priority")]
    [Microsoft.EntityFrameworkCore.Index(nameof(Qname), Name = "IX_qname")]
    public partial class Ssmq
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        [Required]
        [Column("qname")]
        [StringLength(255)]
        public string Qname { get; set; }
        [Required]
        [Column("itemid")]
        [StringLength(500)]
        public string Itemid { get; set; }
        [Column("itemstatus")]
        public int Itemstatus { get; set; }
        [Required]
        [Column("itemvalue")]
        public string Itemvalue { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Required]
        [Column("createdby")]
        [StringLength(255)]
        public string Createdby { get; set; }
        [Column("priority")]
        public int Priority { get; set; }
        [Column("changed", TypeName = "datetime")]
        public DateTime Changed { get; set; }
        [Required]
        [Column("changedBy")]
        [StringLength(255)]
        public string ChangedBy { get; set; }
    }
}
