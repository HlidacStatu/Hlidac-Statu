using Microsoft.EntityFrameworkCore;

namespace HlidacStatu.Entities.Views
{
    [Keyless]
    public class StranaDto
    {
        public string IcoStrany { get; set; }
        public string KratkyNazev { get; set; }
    }
}