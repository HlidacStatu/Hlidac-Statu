using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class Bookmark
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        [StringLength(256)]
        public string UserId { get; set; }
        [Required]
        [StringLength(50)]
        public string Folder { get; set; }
        [Required]
        [StringLength(250)]
        public string Name { get; set; }
        [Required]
        [StringLength(4000)]
        public string Url { get; set; }
        public int ItemType { get; set; }
        [Required]
        [StringLength(256)]
        public string ItemId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
    }
}
