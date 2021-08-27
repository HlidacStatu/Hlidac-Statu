using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("InDocTables")]
    public partial class InDocTables
    {
        [Key]
        public long Pk { get; set; }
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime Created { get; set; }
        [Required]
        [StringLength(20)]
        public string SmlouvaID { get; set; }
        [Required]
        [StringLength(90)]
        public string PrilohaHash { get; set; }

        [Required]
        public string Json { get; set; }

        [Required]
        public int Page { get; set; }
        [Required]
        public int TableOnPage { get; set; }
        [Required]
        [StringLength(50)]
        public string Algorithm { get; set; }
        [Required]
        public decimal PrecalculatedScore { get; set; }
        
        public int? PreFoundRows { get; set; }
        public int? PreFoundCols { get; set; }
        public int? PreFoundJobs { get; set; }
        [Required]
        public int Status { get; set; }
        
        [StringLength(250)]
        public string CheckedBy { get; set; }
        
        [Column(TypeName = "datetime")]
        public DateTime? CheckedDate { get; set; }
        
        public string Note { get; set; }
        public string Tags { get; set; }
        public int? CheckElapsedInMs { get; set; }


        public enum CheckStatuses : int
        { 
            WaitingInQueue = 0,
            InProgress = 1,
            Done = 2,
            ForNextReview = 3,
        }

        [NotMapped()]
        public CheckStatuses CheckStatus 
        { 
            get { return (CheckStatuses)this.Status; }
            set { this.Status = (int)value; }
        }

    }
}