using static HlidacStatu.RegistrVozidel.Models.VlastnikProvozovatelVozidla;

namespace HlidacStatu.RegistrVozidel.Models
{
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
                if (_kategorie == default )
                {
                    _kategorie = KategorieVozidla.GetOrDefault(this.Kategorie_vozidla);
                }
                return _kategorie;
            }
        }

        public bool? Aktualni { get; set; }
        public string Kategorie_vozidla { get; set; }
        public string Tovarni_znacka { get; set; }
        public string typ { get; set; }
        public int? Rok_vyroby { get; set; }
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