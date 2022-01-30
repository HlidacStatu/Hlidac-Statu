using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public class InDocJobNameDescription
    {
        [Key]
        public int Pk { get; set; }
        public string JobGrouped { get; set; }
        [StringLength(100)]
        public string Analyza { get; set; }
        public string Classification { get; set; }
        public string jobGroupedDescription { get; set; }
    }
}