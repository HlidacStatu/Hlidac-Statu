using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    public partial class SmlouvyId
    {
        [Key]
        [StringLength(50)]
        public string Id { get; set; }
        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column("updated", TypeName = "datetime")]
        public DateTime Updated { get; set; }
        [Column("active")]
        public int Active { get; set; }

        [Column("icoOdberatele")]
        [StringLength(30)]
        public string IcoOdberatele { get; set; }
    }
}
