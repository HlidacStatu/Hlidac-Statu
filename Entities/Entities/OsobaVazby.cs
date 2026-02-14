using HlidacStatu.DS.Graphs;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static HlidacStatu.Util.Checksum;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("OsobaVazby")]
    [Microsoft.EntityFrameworkCore.Index(nameof(OsobaId), Name = "IX_OsobaVazby_OsobaId")]
    [Microsoft.EntityFrameworkCore.Index(nameof(OsobaId), nameof(VazbakIco), Name = "IX_OsobaVazby_Vazby")]
    [Microsoft.EntityFrameworkCore.Index(nameof(VazbakIco), Name = "_dta_index_OsobaVazby_9_1877581727__K3_2")]
    public class OsobaVazby
    {
        [Key]
        [CheckSumIgnore]
        [Column("pk")]
        public int Pk { get; set; }

        [Column("OsobaID")]
        public int OsobaId { get; set; }

        [Required]
        [Column("VazbakICO")]
        [StringLength(30)]
        public string VazbakIco { get; set; }

        public int? TypVazby { get; set; }

        [StringLength(150)]
        public string PojmenovaniVazby { get; set; }

        [Column("podil", TypeName = "decimal(18, 9)")]
        public decimal? Podil { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DatumOd { get; set; }

        [Column(TypeName = "date")]
        public DateTime? DatumDo { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ZapisOROd { get; set; }

        [Column(TypeName = "date")]
        public DateTime? ZapisORDo { get; set; }

        [StringLength(50)]
        [CheckSumIgnore]
        public string Zdroj { get; set; }

        [Column(TypeName = "date")]
        [CheckSumIgnore]
        public DateTime LastUpdate { get; set; }

        [CheckSumIgnore]
        public int? VazbakOsobaId { get; set; }

        public int? RucniZapis { get; set; }

        [StringLength(500)]
        [CheckSumIgnore]
        public string Poznamka { get; set; }

        [NotMapped]
        [CheckSumIgnore]
        public Relation.RelationEnum Vazba
        {
            get
            {
                return (Relation.RelationEnum)TypVazby;
            }
            set
            {
                TypVazby = (int)value;
            }
        }


        string _crc = null;
        [CheckSumIgnore]
        [StringLength(66)]
        public string CRC
        {
            get
            {
                if (_crc == null)
                {
                    _crc = HlidacStatu.Util.Checksum.ObjectCheckSum(this);
                }
                return _crc;
            }
            set => _crc = value;
        }

    }
}
