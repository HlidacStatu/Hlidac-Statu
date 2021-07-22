using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("ClassificationOverride")]
    public partial class ClassificationOverride
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string IdSmlouvy { get; set; }
        public int? OriginalCat1 { get; set; }
        public int? OriginalCat2 { get; set; }
        public int? CorrectCat1 { get; set; }
        public int? CorrectCat2 { get; set; }
        [StringLength(150)]
        public string CreatedBy { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Created { get; set; }
    }
}
