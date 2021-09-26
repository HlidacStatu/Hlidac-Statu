using Newtonsoft.Json.Linq;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("InDocTables")]
    public partial class InDocTables
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

        private string[][] _parsedContent = null;
        public string[][] ParsedContent()
        {
            if (_parsedContent == null)
            {
                string[][] cells = new string[0][];

                var json = JArray.Parse(Json);
                var numRows = json.Count;

                if (numRows > 0)
                {
                    cells = new string[numRows][];
                    for (int r = 0; r < numRows; r++)
                    {
                        var row = (JObject)json[r];
                        cells[r] = new string[row.Count];
                        for (int c = 0; c < row.Count; c++)
                        {
                            cells[r][c] = row.GetValue(c.ToString()).Value<string>();
                        }

                    }
                }
                _parsedContent = cells;
            }
            return _parsedContent;
        }


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

        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        public int Year { get; set; }


        public enum CheckStatuses : int
        {
            WaitingInQueue = 0,
            InProgress = 1,
            Done = 2,
            ForNextReview = 3,
            WrongTable = 4,
        }

        [NotMapped()]
        public CheckStatuses CheckStatus
        {
            get { return (CheckStatuses)Status; }
            set { Status = (int)value; }
        }

    }
}