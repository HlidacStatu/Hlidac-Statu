using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("TipUrl")]
    public partial class TipUrl
    {
        [Key]
        [StringLength(150)]
        public string Name { get; set; }
        [Required]
        [StringLength(1000)]
        public string Url { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        [StringLength(150)]
        public string CreatedBy { get; set; }
    }
}
