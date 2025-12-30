using System.Collections.Generic;

namespace HlidacStatu.RegistrVozidel.Models
{
    public class VinDetailViewModel
    {
        public VypisVozidel Vozidlo { get; init; }
        public IReadOnlyCollection<TechnickeProhlidky> TechnickeProhlidky { get; init; } = new List<TechnickeProhlidky>();
        public IReadOnlyCollection<VlastnikProvozovatelVozidla> OwnersHistory { get; init; } = new List<VlastnikProvozovatelVozidla>();
    }
}
