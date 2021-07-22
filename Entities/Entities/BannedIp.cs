using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("BannedIPs")]
    public partial class BannedIp
    {
        [Key]
        [Column("IP")]
        [StringLength(50)]
        public string Ip { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Expiration { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        public int? LastStatusCode { get; set; }
        public string PathList { get; set; }
    }
}
