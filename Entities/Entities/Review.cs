using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Review")]
    public class Review
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [Column("itemType")]
        [StringLength(150)]
        public string itemType { get; set; } //migrace: fujfujfuj :) překrývá se s ItemType v partial, proto tady musí být s malým
        [Column("oldValue")]
        public string OldValue { get; set; }
        [Required]
        [Column("newValue")]
        public string NewValue { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Required]
        [Column("createdBy")]
        [StringLength(150)]
        public string CreatedBy { get; set; }
        [Column("reviewed", TypeName = "datetime")]
        public DateTime? Reviewed { get; set; }
        [Column("reviewedBy")]
        [StringLength(150)]
        public string ReviewedBy { get; set; }
        [Column("reviewResult")]
        public int? ReviewResult { get; set; }
        [Column("comment")]
        [StringLength(500)]
        public string Comment { get; set; }

        public enum ReviewAction
        {
            Denied = 0,
            Accepted = 1,
        }

        public enum ItemTypes
        {
            osobaPhoto,
            osobaPopis,
            other
        }

        [NotMapped]
        public ItemTypes ItemType
        {
            get
            {
                if (string.IsNullOrEmpty(itemType))
                    return ItemTypes.other;
                return Enum.Parse<ItemTypes>(itemType);
            }
            set
            {
                itemType = value.ToString();
            }
        }
    }
}
