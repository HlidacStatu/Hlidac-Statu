using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("WatchDog")]
    [Microsoft.EntityFrameworkCore.Index(nameof(UserId), Name = "IX_WatchDog_UserId")]
    public partial class WatchDog
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(128)]
        public string UserId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Expires { get; set; }
        public int StatusId { get; set; }
        [Required]
        public string SearchTerm { get; set; }
        public string SearchRawQuery { get; set; }
        public int RunCount { get; set; }
        public int SentCount { get; set; }
        public int PeriodId { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastSent { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LastSearched { get; set; }
        public int ToEmail { get; set; }
        public int ShowPublic { get; set; }
        [StringLength(50)]
        public string Name { get; set; }
        public int FocusId { get; set; }
        [Column("dataType")]
        [StringLength(50)]
        public string DataType { get; set; }
        public string SpecificContact { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? LatestRec { get; set; }
    }
}
