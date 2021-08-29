using Microsoft.EntityFrameworkCore;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("Osoba")]
    [Index(nameof(NameId), Name = "idx_Osoba_nameId")]
    [Index(nameof(JmenoAscii), nameof(PrijmeniAscii), nameof(Narozeni), Name = "idx_osoba_jmenoAsciiPrijmeniAsciiNarozeni")]
    [Index(nameof(Jmeno), nameof(Prijmeni), nameof(Narozeni), Name = "idx_osoba_jmenoPrijmeniNarozeni")]
    [Index(nameof(JmenoAscii), Name = "idx_osoba_jmenoascii")]
    [Index(nameof(PrijmeniAscii), Name = "idx_osoba_prijmeniascii")]
    public partial class Osoba
    {
        [Key]
        public int InternalId { get; set; }
        [StringLength(50)]
        public string TitulPred { get; set; }
        [Required]
        [StringLength(150)]
        public string Jmeno { get; set; }
        [Required]
        [StringLength(150)]
        public string Prijmeni { get; set; }
        [StringLength(50)]
        public string TitulPo { get; set; }
        [StringLength(1)]
        public string Pohlavi { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Narozeni { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Umrti { get; set; }
        [StringLength(500)]
        public string Ulice { get; set; }
        [StringLength(150)]
        public string Mesto { get; set; }
        [Column("PSC")]
        [StringLength(25)]
        public string Psc { get; set; }
        [StringLength(5)]
        public string CountryCode { get; set; }
        public bool OnRadar { get; set; }
        public int Status { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime LastUpdate { get; set; }
        [StringLength(50)]
        public string NameId { get; set; }
        [StringLength(150)]
        public string PuvodniPrijmeni { get; set; }
        [StringLength(150)]
        public string JmenoAscii { get; set; }
        [StringLength(150)]
        public string PrijmeniAscii { get; set; }
        [StringLength(150)]
        public string PuvodniPrijmeniAscii { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? ManuallyUpdated { get; set; }
        [StringLength(150)]
        public string ManuallyUpdatedBy { get; set; }
        [StringLength(20)]
        public string WikiId { get; set; }
        public int? OriginalId { get; set; }
    }
}
