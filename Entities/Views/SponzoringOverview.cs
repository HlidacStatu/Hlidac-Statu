using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views
{
    [Keyless]
    public class SponzoringOverview
    {
        public string KratkyNazev { get; set; }
        public string IcoStrany { get; set; }
        public int Rok { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal? DaryCelkem { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal? DaryOsob { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal? DaryFirem { get; set; }
        public int PocetDarujicichOsob { get; set; }
        public int PocetDarujicichFirem { get; set; }
    }
}