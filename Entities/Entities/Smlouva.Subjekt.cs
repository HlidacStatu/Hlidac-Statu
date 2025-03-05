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
                identifikace = s.identifikace;
            }

            public Subjekt(tSmlouvaSubjekt s)
            {
                adresa = s.adresa;
                datovaSchranka = s.datovaSchranka;
                ico = s.ico;
                nazev = s.nazev;
                utvar = s.utvar;
                identifikace = s.identifikace;
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

            ///  Identifikace smluvní strany
            /// FO, PFO, PO, OVM, ZFO (zahr.FO), ZPO (zahr.PO)
            [Keyword()]
            public string identifikace { get; set; }

            public string IdentifikaceFull()
            {
                switch (this.identifikace?.ToLowerInvariant())
                {
                    case "fo":
                        return "fyzická osoba";
                    case "pfo":
                        return "podnikatel";
                    case "po":
                        return "právnická osoba";
                    case "ovm":
                        return "orgán veřejné moci";
                    case "zfo":
                        return "zahraniční fyzická osoba";
                    case "zpo":
                        return "zahraniční právnická osoba";
                    default:
                        return string.Empty;
                        break;
                }


            }
        }
    }
}
