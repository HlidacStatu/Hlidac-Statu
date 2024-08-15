using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class AutocompleteSynonym
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        [Required]
        [Column("text")]
        public string Text { get; set; }
        [Required]
        [Column("description")]
        public string Description { get; set; }
        [Required]
        [Column("query")]
        public string Query { get; set; }
        [Column("queryPlaty")]
        public string QueryPlaty { get; set; }
        [Required]
        [Column("type")]
        public string Type { get; set; }
        [Column("priority")]
        public int Priority { get; set; }
        [Required]
        [Column("imageElement")]
        public string ImageElement { get; set; }
        [Column("active")]
        public int Active { get; set; }
    }
}
