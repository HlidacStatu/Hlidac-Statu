using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("ItemToOcrQueue")]
    [Microsoft.EntityFrameworkCore.Index(nameof(Done), nameof(Started), Name = "idx_ItemToOcrQueue_doneStarted")]
    [Microsoft.EntityFrameworkCore.Index(nameof(ItemType), nameof(ItemId), nameof(Done), nameof(Started), Name = "idx_ItemToOcrQueue_many")]
    public partial class ItemToOcrQueue
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        [Required]
        [Column("itemType")]
        [StringLength(100)]
        public string ItemType { get; set; }
        [Column("itemSubType")]
        [StringLength(100)]
        public string ItemSubType { get; set; }
        [Required]
        [Column("itemId")]
        [StringLength(150)]
        public string ItemId { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column("done", TypeName = "datetime")]
        public DateTime? Done { get; set; }
        [Column("started", TypeName = "datetime")]
        public DateTime? Started { get; set; }
        [Column("result")]
        public string Result { get; set; }
        [Column("success")]
        public int? Success { get; set; }
        [Column("priority")]
        public int? Priority { get; set; }
    }
}
