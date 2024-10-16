﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System;

namespace HlidacStatu.Entities
{
    [Table("QAITask")]

    public partial class QAITask
    {
        public const int DefaultPriority = 10;

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
        [Column("callerTaskType")]
        [StringLength(64)]
        public string CallerTaskType { get; set; }

        [Column("source")]
        public string Source { get; set; }

        public T GetOptions<T>()
    where T : class
        {
            try
            {
                if (!string.IsNullOrEmpty(OptionsRaw))
                    return System.Text.Json.JsonSerializer.Deserialize<T>(this.OptionsRaw);
                else
                    return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SetOptions<T>(T data)
            where T : class
        {
            if (data != null)
                this.OptionsRaw = System.Text.Json.JsonSerializer.Serialize(data);
            else
                this.OptionsRaw = null;

        }
        
        [Column("options")]
        public string OptionsRaw { get; set; }

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

        [Column("resulttype")]
        [StringLength(64)]
        public string ResultType { get; set; }

        [Column("status")]
        public int? Status { get; set; }

        [Column("priority")]
        public int? Priority { get; set; } = DefaultPriority;

        [Column("processEngine")]
        public string ProcessEngine { get; set; }



    }

}
