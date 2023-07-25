using System.Collections.Generic;

namespace HlidacStatu.Entities
{
    public partial class SkutecnyMajitel
    {
        [Nest.Keyword]
        public string id { get; set; }

        [Nest.Keyword]
        public string ico { get; set; }
        
        [Nest.Text]
        public string nazev_subjektu { get; set; }

        [Nest.Object]
        public List<Majitel> skutecni_majitele { get; set; }

        public class Majitel
        {
            [Nest.Text]
            public string datum_zapis { get; set; }
            [Nest.Text]
            public string datum_vymaz { get; set; }
            [Nest.Text]
            public string postaveni { get; set; }
            [Nest.Text]
            public string osobaId { get; set; }
            [Nest.Text]
            public string osoba_jmeno { get; set; }
            [Nest.Text]
            public string osoba_prijmeni { get; set; }
            [Nest.Text]
            public string osoba_datum_narozeni { get; set; }
            [Nest.Text]
            public string osoba_titul_pred { get; set; }
            [Nest.Text]
            public string podil_na_prospechu_typ { get; set; }
            [Nest.Text]
            public string podil_na_prospechu_hodnota { get; set; }
        }
    }
}