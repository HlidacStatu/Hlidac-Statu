using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public class InDocJobNames
    {
        [Key]
        public int Pk { get; set; }
        public string JobRaw { get; set; }
        public string JobGrouped { get; set; }
        [StringLength(100)]
        public string Subject { get; set; }
        public string Tags { get; set; }
    }
}