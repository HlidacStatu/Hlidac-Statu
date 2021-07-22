using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class UserOptions
    {
        [Key]
        [Column("pk")]
        public int Pk { get; set; }
        [StringLength(128)]
        public string UserId { get; set; }
        public int OptionId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        public string Value { get; set; }
        public int? LanguageId { get; set; }
    }
}
