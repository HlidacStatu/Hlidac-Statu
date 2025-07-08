using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Web.Models
{
    public class ProslovyContext : DbContext
    {
        public ProslovyContext(DbContextOptions<ProslovyContext> options) : base(options) { }

        public DbSet<ProslovyModel> Proslovy { get; set; }
    }

    [Table("Stenozaznamy")]
    public class ProslovyModel
    {
        [Key]
        public string Id { get; set; }
        public int? poradi { get; set; }
        public int? obdobi { get; set; }
        public DateTime? datum { get; set; }
        public int? schuze { get; set; }
        public string? url { get; set; }
        public int? cisloHlasovani { get; set; }
        public string? celeJmeno { get; set; }
        public DateTime? narozeni { get; set; }
        public string? HsProcessType { get; set; }
        public string? OsobaId { get; set; }
        public string? funkce { get; set; }
        public string? tema { get; set; }
        public string text { get; set; }
        public int? pocetSlov { get; set; }
        public string? politiciZminky { get; set; }
        public string? temata { get; set; }
        public double? dobaProslovuSec { get; set; }
        public string? politickaStrana { get; set; }
    }
}
