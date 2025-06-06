﻿using System.ComponentModel.DataAnnotations;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class ObceZUJAttrName
    {
        [Key]
        [StringLength(50)]
        public string Key { get; set; }


        [Required]
        [StringLength(250)]
        public string Keyname { get; set; }

        [Required]
        [StringLength(10)]
        public string Type { get; set; }

    }
}
