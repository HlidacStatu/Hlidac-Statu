using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Devmasters.Enums;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("CenyCustomer")]
    public partial class CenyCustomer
    {

        [Required]
        public string Username { get; set; }

        [Required]
        public string Analyza { get; set; }

        [Required]
        public int Level { get; set; }

        [NotMapped]
        public AccessDetail.AccessDetailLevel AccessLevel { get => (AccessDetail.AccessDetailLevel)this.Level; }

        [Required]
        public int Rok { get; set; }

        [Required]
        public decimal Amount { get; set; }


        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? Paid { get; set; }


    }
}