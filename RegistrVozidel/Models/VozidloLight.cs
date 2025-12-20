using Devmasters.DT;
using static HlidacStatu.RegistrVozidel.Models.VlastnikProvozovatelVozidla;

namespace HlidacStatu.RegistrVozidel.Models
{

    public class VozidloLightPerIco : VozidloLight
    {
        public VozidloLightPerIco()
        {
        }
        public VozidloLightPerIco(IEnumerable<VozidloLight> vozidlaLight)
        {
            if (vozidlaLight == null)
                throw new ArgumentNullException("vozidlaLight");
            if (vozidlaLight.DistinctBy(m => m.Ico).Count() > 1)
                throw new ArgumentNullException("More different ICO is not allowed");

            List<DateInterval> majitel = new List<DateInterval>();
            List<DateInterval> provozovatel = new List<DateInterval>();

            foreach (var v in vozidlaLight)
            {
                if (v.FormaVlastnictvi == Enums.Vztah_k_vozidluEnum.Neuveden || v.FormaVlastnictvi == Enums.Vztah_k_vozidluEnum.Vlastnik)
                    majitel.Add(new DateInterval(v.DatumOd?.ToDateTime(TimeOnly.MinValue), v.DatumDo?.ToDateTime(TimeOnly.MinValue)));
                else
                    provozovatel.Add(new DateInterval(v.DatumOd?.ToDateTime(TimeOnly.MinValue), v.DatumDo?.ToDateTime(TimeOnly.MinValue)));
            }
            var v2 = vozidlaLight.Last();
            Pcv = v2.Pcv;
            Ico = v2.Ico;
            Typ_subjekt = v2.Typ_subjekt;
            Vztah_k_vozidlu = v2.Vztah_k_vozidlu;
            DatumOd = v2.DatumOd;
            DatumDo = v2.DatumDo;
            Aktualni = v2.Aktualni;
            Kategorie_vozidla = v2.Kategorie_vozidla;
            Tovarni_znacka = v2.Tovarni_znacka;
            Typ = v2.Typ;
            Rok_vyroby = v2.Rok_vyroby;
            Palivo = v2.Palivo;
            Datum_1_registrace = v2.Datum_1_registrace;
            Datum_1_registrace_v_CR = v2.Datum_1_registrace_v_CR;
            Zdvihovy_objem = v2.Zdvihovy_objem;
            Barva = v2.Barva;
            Nejvyssi_rychlost = v2.Nejvyssi_rychlost;
            PlneElektrickeVozidlo = v2.PlneElektrickeVozidlo;
            HybridniVozidlo = v2.HybridniVozidlo;
            Stupen_plneni_emisni_urovne = v2.Stupen_plneni_emisni_urovne;
            ProvozniHmotnost = v2.ProvozniHmotnost;

            this.Majitel_v_dobe = majitel.OrderBy(o => o.From ?? DateTime.MinValue).ToArray();
            this.Provozovatel_v_dobe = provozovatel.OrderBy(o => o.From ?? DateTime.MinValue).ToArray();

        }


        public DateInterval[] Majitel_v_dobe { get; set; } = Array.Empty<DateInterval>();
        public DateInterval[] Provozovatel_v_dobe { get; set; } = Array.Empty<DateInterval>();

        Enums.Vztah_k_vozidluEnum[] _formy_vlastnictvi = null;
        public Enums.Vztah_k_vozidluEnum[] FormyVlastnictvi
        {
            get
            {
                if (_formy_vlastnictvi == null)
                {
                    var fv = new List<Enums.Vztah_k_vozidluEnum>();
                    if (Majitel_v_dobe.Length > 0)
                        fv.Add(Enums.Vztah_k_vozidluEnum.Vlastnik);
                    if (Provozovatel_v_dobe.Length > 0)
                        fv.Add(Enums.Vztah_k_vozidluEnum.Provozovatel);
                    _formy_vlastnictvi = fv.ToArray();
                }
                return _formy_vlastnictvi;
            }
        }
    }
    public class VozidloLight
    {
        public string Pcv { get; set; }
        public string Ico { get; set; }
        public decimal? Typ_subjekt { get; set; }
        public decimal? Vztah_k_vozidlu { get; set; }
        public Enums.Vztah_k_vozidluEnum FormaVlastnictvi { get => (Enums.Vztah_k_vozidluEnum)(Vztah_k_vozidlu ?? 0); }

        public DateOnly? DatumOd { get; set; }
        public DateOnly? DatumDo { get; set; }


        KategorieVozidlaInfo _kategorie = default;
        public KategorieVozidlaInfo Kategorie
        {
            get
            {
                if (_kategorie == default)
                {
                    _kategorie = KategorieVozidla.GetOrDefault(this.Kategorie_vozidla);
                }
                return _kategorie;
            }
        }

        public bool? Aktualni { get; set; }
        public string Kategorie_vozidla { get; set; }
        public string Tovarni_znacka { get; set; }
        public string Typ { get; set; }
        public int? Rok_vyroby { get; set; }
        public string Palivo { get; set; }
        public DateOnly? Datum_1_registrace { get; set; }
        public DateOnly? Datum_1_registrace_v_CR { get; set; }
        public string Zdvihovy_objem { get; set; }
        public string Barva { get; set; }
        public decimal? Nejvyssi_rychlost { get; set; }
        public bool? PlneElektrickeVozidlo { get; set; }
        public bool? HybridniVozidlo { get; set; }
        public string Stupen_plneni_emisni_urovne { get; set; }
        public string ProvozniHmotnost { get; set; }
    }
}