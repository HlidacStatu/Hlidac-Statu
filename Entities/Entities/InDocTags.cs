using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("InDocTags")]
    public class InDocTag
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int Pk { get; set; }

        [Required]
        [StringLength(500)]
        public string Keyword { get; set; }

        [Required]
        [StringLength(500)]
        public string Tag { get; set; }
        [Required]
        [StringLength(150)]
        public string Analyza { get; set; }
    }
}