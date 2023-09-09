using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{


    public partial class ObceZUJAttr
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Pk { get; set; }

        [Required]
        [StringLength(50)]
        public string Key { get; set; }

        public int? Year{ get; set; }

        public decimal? Value { get; set; }

        [Required]
        [StringLength(50)]
        public string Ico { get; set; }

    }
}
