using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations.Schema;

namespace HlidacStatu.Entities.Views
{
    [Keyless]
    public class SponzoringSummed
    {

        public string Id { get; set; }
        public string Jmeno { get; set; }
        public int Rok { get; set; }
        [Column(TypeName = "decimal(18, 9)")]
        public decimal DarCelkem { get; set; }
        public string IcoStrany { get; set; }
        public string NazevStrany { get; set; }
    }
}