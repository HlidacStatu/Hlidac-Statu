using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("qVoiceToText")]
    public partial class QVoiceToText
    {
        [Key]
        [Column("qId")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long QId { get; set; }

        [Required]
        [Column("callerId")]
        [StringLength(64)]
        public string CallerId { get; set; }

        [Required]
        [Column("callerTaskId")]
        [StringLength(64)]
        public string CallerTaskId { get; set; }


        [Required]
        [Column("source")]
        public string Source { get; set; }

        public T GetSourceOptions<T>()
            where T : class
        {
            try
            {
                if (!string.IsNullOrEmpty(SourceOptionsRaw))
                    return System.Text.Json.JsonSerializer.Deserialize<T>(this.SourceOptionsRaw);
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }




        public void SetSourceOptions<T>(T data)
            where T: class
        {
            if (data != null)
                this.SourceOptionsRaw = System.Text.Json.JsonSerializer.Serialize(data);
            else
                this.SourceOptionsRaw = null;

        }

        [Required]
        [Column("sourceOptions")]
        public string SourceOptionsRaw { get; set; }

        [Column("created", TypeName = "datetime")]
        public DateTime Created { get; set; } = DateTime.Now;

        [Column("done", TypeName = "datetime")]
        public DateTime? Done { get; set; }

        [Column("started", TypeName = "datetime")]
        public DateTime? Started { get; set; } 

        [Column("lastUpdate", TypeName = "datetime")]
        public DateTime? LastUpdate { get; set; }

        [Column("result")]
        public string Result { get; set; }

        [Column("status")]
        public int? Status { get; set; }

        [Column("priority")]
        public int? Priority { get; set; }

    }
}
