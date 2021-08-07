using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;

using Microsoft.EntityFrameworkCore;

#nullable disable

namespace HlidacStatu.Entities
{
    [Table("UcetniJednotka")]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public partial class UcetniJednotka
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [Column("ico")]
        [StringLength(20)]
        public string Ico { get; set; }
        [Column("start_date")]
        public DateTime? StartDate { get; set; }
        [Column("end_date")]
        public DateTime? EndDate { get; set; }
        [Column("ucjed_nazev")]
        [StringLength(300)]
        public string UcjedNazev { get; set; }
        [Column("dic")]
        [StringLength(50)]
        public string Dic { get; set; }
        [Column("adresa")]
        [StringLength(300)]
        public string Adresa { get; set; }
        [Column("nuts_id")]
        [StringLength(50)]
        public string NutsId { get; set; }
        [Column("zrizovatel_ico")]
        [StringLength(150)]
        public string ZrizovatelIco { get; set; }
        [Column("cofog_id")]
        public int? CofogId { get; set; }
        [Column("isektor_id")]
        public int? IsektorId { get; set; }
        [Column("kapitola_id")]
        public int? KapitolaId { get; set; }
        [Column("nace_id")]
        public int? NaceId { get; set; }
        [Column("druhuj_id")]
        public int? DruhujId { get; set; }
        [Column("poddruhuj_id")]
        public int? PoddruhujId { get; set; }
        [Column("konecplat")]
        public DateTime? Konecplat { get; set; }
        [Column("forma_id")]
        public int? FormaId { get; set; }
        [Column("katobyv_id")]
        public int? KatobyvId { get; set; }
        [Column("obec")]
        [StringLength(150)]
        public string Obec { get; set; }
        [Column("kraj")]
        [StringLength(50)]
        public string Kraj { get; set; }
        [Column("stat_id")]
        public int? StatId { get; set; }
        [Column("zdrojfin_id")]
        public int? ZdrojfinId { get; set; }
        [Column("druhrizeni_id")]
        public int? DruhrizeniId { get; set; }
        [Column("veduc_id")]
        public int? VeducId { get; set; }
        [Column("zuj")]
        [StringLength(50)]
        public string Zuj { get; set; }
        [Column("sidlo")]
        [StringLength(150)]
        public string Sidlo { get; set; }
        [Column("zpodm_id")]
        public int? ZpodmId { get; set; }
        [Column("kod_pou")]
        public int? KodPou { get; set; }
        [Column("typorg_id")]
        public int? TyporgId { get; set; }
        [Column("pocob")]
        public int? Pocob { get; set; }
        [Column("ulice")]
        [StringLength(250)]
        public string Ulice { get; set; }
        [Column("kod_rp")]
        public int? KodRp { get; set; }
        [Column("datumakt")]
        public DateTime? Datumakt { get; set; }
        [Column("aktorg_id")]
        public int? AktorgId { get; set; }
        [Column("datumvzniku")]
        public DateTime? Datumvzniku { get; set; }
        [Column("psc")]
        [StringLength(20)]
        public string Psc { get; set; }

        private string GetDebuggerDisplay()
        {
            return $"{ZrizovatelIco}->({StartDate:yyyy-MM-dd}-{EndDate:yyyy-MM-dd})->{Ico}";
        }
    }
}
