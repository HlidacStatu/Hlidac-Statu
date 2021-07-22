using HlidacStatu.Entities.XSD;
using Nest;

namespace HlidacStatu.Entities
{

    public partial class Smlouva
    {
        public class Subjekt
        {
            public Subjekt() { }

            public Subjekt(tSmlouvaSmluvniStrana s)
            {
                adresa = s.adresa;
                datovaSchranka = s.datovaSchranka;
                ico = s.ico;
                nazev = s.nazev;
                utvar = s.utvar;
            }

            public Subjekt(tSmlouvaSubjekt s)
            {
                adresa = s.adresa;
                datovaSchranka = s.datovaSchranka;
                ico = s.ico;
                nazev = s.nazev;
                utvar = s.utvar;
            }

            public string adresa { get; set; }

            [Keyword()]
            public string datovaSchranka { get; set; }
            [Keyword()]
            public string ico { get; set; }

            [Keyword()]
            public string nazev { get; set; }
            [Keyword()]
            public string utvar { get; set; }
        }
    }
}
