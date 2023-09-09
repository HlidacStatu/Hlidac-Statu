using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{

    public partial class ObceZUJ
    {
        [Key]
        public string Ico { get; set; }

        [Required]
        public int ObecKod { get; set; }


        [Required]
        [StringLength(512)]
        public string ObecNazev { get; set; }

        [Required]
        [StringLength(512)]
        public string ObecStatus { get; set; }


        [Required]
        public int Obec_s_poverenym_OU_kod { get; set; }
        [Required]
        [StringLength(512)]
        public string Obec_s_poverenym_OU_nazev { get; set; }


        [Required]
        public int Obec_s_rozsirenou_pusobnosti_kod { get; set; }
        [Required]
        [StringLength(512)]
        public string Obec_s_rozsirenou_pusobnosti_nazev { get; set; }

        [Required]
        [StringLength(512)]
        public string Okres_kod { get; set; }
        [Required]
        [StringLength(512)]
        public string Okres_nazev { get; set; }

        [Required]
        [StringLength(512)]
        public string Kraj_kod { get; set; }
        [Required]
        [StringLength(512)]
        public string Kraj_nazev { get; set; }


        [Required]
        [StringLength(512)]
        public string Region_soudrznosti_kod { get; set; }
        [Required]
        [StringLength(512)]
        public string Region_soudrznosti_nazev { get; set; }
    }
}
