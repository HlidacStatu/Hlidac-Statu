using HlidacStatu.Entities;

namespace HlidacStatuApi.Models
{
    public class FirmaSocialDTO
    {
        public FirmaSocialDTO(Firma firma, List<OsobaEvent> events)
        {
            this.Jmeno = firma.Jmeno;
            this.Ico = firma.ICO;
            this.Profile = firma.GetUrl();
            this.SocialniSite = events.Select(e => new SocialNetworkDTO(e)).ToList();
        }
        public string Ico { get; set; }
        public string Jmeno { get; set; }
        public string Profile { get; set; }
        public List<SocialNetworkDTO> SocialniSite { get; set; }
    }
}