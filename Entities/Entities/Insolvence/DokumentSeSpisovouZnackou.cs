using HlidacStatu.Entities.Insolvence;

namespace HlidacStatu.Entities.Insolvence
{
    public class DokumentSeSpisovouZnackou
    {
        public Dokument Dokument { get; set; }
        public string SpisovaZnacka { get; set; }
        public string UrlId { get; set; }
        public Rizeni Rizeni { get; set; }
    }
}
