using HlidacStatu.Entities;

namespace HlidacStatu.Web.Models
{
    public class RenderOsobaBoxViewModel
    {
        public Osoba Osoba { get; set; }
        public bool Smaller { get; set; }

        public RenderOsobaBoxViewModel(Osoba osoba, bool smaller)
        {
            Osoba = osoba;
            Smaller = smaller;
        }
    }
    
}