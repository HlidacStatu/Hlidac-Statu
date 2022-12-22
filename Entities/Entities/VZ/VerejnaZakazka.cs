using Devmasters.Enums;

using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Xml.Linq;
using Result = HlidacStatu.Lib.OCR.Api.Result;

namespace HlidacStatu.Entities.VZ

{
    [Description("Struktura verejne zakazky v Hlidaci Statu")]
    public partial class VerejnaZakazka
        : IBookmarkable, IFlattenedExport
    {
        public class Sources
        {
            public const string VestnikVerejnychZakazek = "https://vestnikverejnychzakazek.cz/";
            public const string Datlab = "https://datlab.eu/";
            public const string Rozza = "https://rozza.cz/";
        }
        
        public const string Pre2016Dataset = "VVZ-2006";
        public const string Post2016Dataset = "VVZ-2016";
        public const string ProfileOnlyDataset = "VVZ-Profil";


        [Description("Formuláře spojené s touto zakázkou. Vychazi z XML na VVZ z www.isvz.cz/ISVZ/MetodickaPodpora/Napovedaopendata.pdf")]
        public class Formular : IEquatable<Formular>
        {
            [Description("Číslo formuláře")]
            [Keyword]
            public string Cislo { get; set; } = string.Empty;

            [Description("Druh formuláře (F01-F31, CZ01-CZ02)")]
            [Keyword]
            public string Druh { get; set; } = string.Empty;

            [Keyword]
            [Description("Typ formuláře (řádný/opravný), nebo nevyplněno (pak je řádný)")]
            public string TypFormulare { get; set; } = string.Empty;

            [Keyword]
            [Description("Limit VZ (nadlimitní/podlimitní/VZMR)")]
            public string LimitVZ { get; set; } = string.Empty;

            [Keyword]
            [Description("Druh řízení dle ZVZ")]
            public string DruhRizeni { get; set; } = string.Empty;

            public DruhyFormularu DruhFormulare()
            {
                DruhyFormularu druh;
                if (Enum.TryParse<DruhyFormularu>(Druh, out druh))
                    return druh;
                else
                    return DruhyFormularu.Unknown;
            }
            public string DruhFormulareName()
            {
                if (DruhFormulare() == DruhyFormularu.Unknown)
                    return "";
                else
                    return DruhFormulare().ToNiceDisplayName().Trim();
            }

            [Description("Datum zveřejnění.")]
            [Date]
            public DateTime? Zverejnen { get; set; }

            [Description("URL formuláře, může být prázdné")]
            [Keyword(Index = false)]
            public String URL { get; set; } = string.Empty;


            //[Boolean]
            //public bool 
            public bool Equals(Formular other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(Cislo, other.Cislo, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(Druh, other.Druh, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(TypFormulare, other.TypFormulare, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(LimitVZ, other.LimitVZ, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(DruhRizeni, other.DruhRizeni, StringComparison.InvariantCultureIgnoreCase) &&
                       Nullable.Equals(Zverejnen, other.Zverejnen) &&
                       string.Equals(URL, other.URL, StringComparison.InvariantCultureIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Formular)obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Cislo, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(Druh, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(TypFormulare, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(LimitVZ, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(DruhRizeni, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(Zverejnen);
                hashCode.Add(URL, StringComparer.InvariantCultureIgnoreCase);
                return hashCode.ToHashCode();
            }
        }

        [Description("Druh formuláře - mezinárodní (F*) i české (CZ*).")]
        [ShowNiceDisplayName]
        public enum DruhyFormularu
        {
            [NiceDisplayName("Předběžné oznámení")]
            F01,
            [NiceDisplayName("Oznámení o zahájení zadávacího řízení")]
            F02,
            [NiceDisplayName("Oznámení o výsledku zadávacího řízení")]
            F03,
            [NiceDisplayName("Pravidelné předběžné oznámení")]
            F04,
            [NiceDisplayName("Oznámení o zahájení zadávacího řízení veřejné služby")]
            F05,
            [NiceDisplayName("Oznámení o výsledku zadávacího řízení veřejné služby")]
            F06,
            [NiceDisplayName("Systém kvalifikace veřejné služby")]
            F07,
            [NiceDisplayName("Oznámení na profilu kupujícího")]
            F08,
            [NiceDisplayName("Oznámení o zahájení soutěže o návrh")]
            F12,
            [NiceDisplayName("Oznámení o výsledcích soutěže o návrh")]
            F13,
            [NiceDisplayName("Oprava ")]
            F14,
            [NiceDisplayName("Oznámení o dobrovolné průhlednosti ex ante ")]
            F15,
            [NiceDisplayName("Oznámení předběžných informací - obrana a bezpečnost ")]
            F16,
            [NiceDisplayName("Oznámení o zakázce - obrana a bezpečnost")]
            F17,
            [NiceDisplayName("Oznámení o zadání zakázky - obrana a bezpečnost")]
            F18,
            [NiceDisplayName("Oznámení o subdodávce - obrana a bezpečnost")]
            F19,
            [NiceDisplayName("Oznámení o změně ")]
            F20,
            [NiceDisplayName("Sociální a jiné zvláštní služby – veřejné zakázky ")]
            F21,
            [NiceDisplayName("Sociální a jiné zvláštní služby – veřejné služby")]
            F22,
            [NiceDisplayName("Sociální a jiné zvláštní služby – koncese")]
            F23,
            [NiceDisplayName("Oznámení o zahájení koncesního řízení ")]
            F24,
            [NiceDisplayName("Oznámení o výsledku koncesního řízení")]
            F25,

            [NiceDisplayName("Předběžné oznámení zadávacího řízení v podlimitním režimu")]
            CZ01,
            [NiceDisplayName("Oznámení o zahájení zadávacího řízení v podlimitním režimu ")]
            CZ02,
            [NiceDisplayName("Oznámení o výsledku zadávacího řízení v podlimitním režimu ")]
            CZ03,
            [NiceDisplayName("Oprava národního formuláře ")]
            CZ04,
            [NiceDisplayName("Oznámení profilu zadavatele")]
            CZ05,
            [NiceDisplayName("Zrušení/zneaktivnění profilu zadavatele")]
            CZ06,
            [NiceDisplayName("??")]
            CZ07,

            [NiceDisplayName("Záznam na profilu zadavatele")]
            ProfilZadavatele,

            [NiceDisplayName("Neznámý")]
            Unknown

        }

        [Description("Odvozené stavy zakázky pro Hlídače.")]
        [ShowNiceDisplayName]
        public enum StavyZakazky
        {
            [NiceDisplayName("Oznámen úmysl vyhlásit tendr")] Umysl = 1,
            [NiceDisplayName("Tendr zahájen")] Zahajeno = 100,
            [NiceDisplayName("Tendr se vyhodnocuje")] Vyhodnocovani = 200,
            [NiceDisplayName("Tendr byl ukončen")] Ukonceno = 300,
            [NiceDisplayName("Tendr byl zrušen")] Zruseno = 50,
            [NiceDisplayName("Nejasný stav")] Jine = 0,
        }

        [Description("Unikátní ID zakázky. Nevyplňujte, ID vygeneruje Hlídač Státu.")]
        public string Id { get; init; } = Guid.NewGuid().ToString("N");
        
        [Keyword()]
        [Description("Interní ID na věstníku veřejných zakázek")]
        public string VvzInternalId { get; set; } = null;

        [Object()]
        [Description("Seznam datasetů a evidenčních čísel v datasetech, kde se VZ vyskytuje." +
                     " Hodnota 'VVZ-2006' pro zakázky z VVZ od 2006-2016, 'VVZ-2016' pro nové dle zákona o VZ z 2016")]
        public HashSet<Zdroj> Zdroje { get; set; } = new();

        public class Zdroj : IEquatable<Zdroj>
        {
            private string _domena;

            [Keyword()]
            public string UniqueId => $"{Domena}_{IdVDomene}_{IsPre2016}";

            [Keyword()]
            public string Domena
            {
                get => _domena;
                set
                {
                    if(Uri.TryCreate(value, UriKind.Absolute, out var uri))
                    {
                        _domena = uri.Host;
                    }
                    else
                    {
                        _domena = string.Empty;
                    }
                }
            }

            [Keyword()]
            public string IdVDomene { get; set; }
            [Boolean]
            public bool IsPre2016 { get; set; } = false;
            [Date()]
            public DateTime PosledniZmenaZdroje { get; set; }
            [Date()]
            public DateTime Modified { get; set; }

            public bool Equals(Zdroj other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(UniqueId, other.UniqueId, StringComparison.InvariantCultureIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Zdroj)obj);
            }

            public override int GetHashCode()
            {
                return UniqueId.GetHashCode(StringComparison.InvariantCultureIgnoreCase);
            }
        }
        
        [Object(Enabled = false)] //tady prohledávat nebudeme, index zatěžovat nepotřebujeme
        public List<string> Changelog { get; set; } = new();

        public string EvidencniCisloZVestniku(bool isPre2016 = false)
        {
            string evidencniCilo = Zdroje.Where(zdroj =>
                    zdroj.IsPre2016 = isPre2016 && zdroj.Domena == Sources.VestnikVerejnychZakazek)
                .Select(zdroj => zdroj.IdVDomene)
                .FirstOrDefault();

            return evidencniCilo;
        }

        public string VypisZdroju(string separator = "\n")
        {
            return string.Join(separator, Zdroje.Select(zdroj => $"{zdroj.Domena}: {zdroj.IdVDomene}"));
        }

        /// <summary>
        /// Vrátí jedno ID ze zdrojů. Pořadí je následující:
        /// <br>1. Public id z profilu zadavatele (pokud je více profilů, vybere se libovolný) </br>
        /// <br>2. Pokud není profil zadavatele, pak bereme id z věstníku veř. zakázek</br>
        /// <br>3. Pokud není nic z výše uvedených, bereme první libovolné id</br>
        /// </summary>
        /// <returns></returns>
        public string ZobrazPrimarniIdZdroje()
        {
            var id = Zdroje.Where(zdroj => zdroj.IdVDomene.StartsWith("P", StringComparison.InvariantCultureIgnoreCase))
                .Select(zdroj => zdroj.IdVDomene)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            id = Zdroje.Where(zdroj =>
                    zdroj.Domena.Equals(Sources.VestnikVerejnychZakazek, StringComparison.InvariantCultureIgnoreCase))
                .Select(zdroj => zdroj.IdVDomene)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(id))
                return id;

            return Zdroje.FirstOrDefault()?.IdVDomene;

        }
        
        [Object()]
        [Description("Externí ID. Seznam dalších různých ID, např. ze systému Rozza")]
        public HashSet<string> ExternalIds { get; set; } = new();

        // [Description("ID profilu zadavatele, na kterém tato zakázka.")]
        // [Keyword()]
        // public string ZakazkaNaProfiluId { get; set; } = null;

        [Description("Všechny formuláře spojené se zakázkou")]
        public HashSet<Formular> Formulare { get; set; } = new();

        [Description("Hodnotící kritéria zakázky")]
        public HashSet<HodnoticiKriteria> Kriteria { get; set; } = new();

        [Description("Nepouzito")]
        [Keyword()]
        public int DisplayId { get; set; }


        [Object()]
        [Description("Zadavatel")]
        public Subject Zadavatel { get; set; }

        [Object()]
        [Description("Dodavatele")]
        public Subject[] Dodavatele { get; set; } = new Subject[] { };

        [Description("Název zakázky")]
        public string NazevZakazky { get; set; }
        [Description("Popis zakázky z VVZ")]
        public string PopisZakazky { get; set; }
        [Description("Popis zakázky z Rozza")]
        public string PopisZakazkyRozza { get; set; }

        [Object()]
        [Description("Zadávací dokumentace a další dokumenty spojené se zakázkou")]
        public List<Document> Dokumenty { get; set; } = new();


        [Keyword()]
        [Description("CPV kódy určující oblast VZ")]
        public HashSet<string> CPV { get; set; } = new();

        [Date()]
        [Description("Datum uveřejnění")]
        public DateTime? DatumUverejneni { get; set; } = null;

        [Date()]
        [Description("Lhůta pro doručení nabídek")]
        public DateTime? LhutaDoruceni { get; set; } = null;
        
        [Date()]
        [Description("Lhůta pro doručení přihlášky k účasti na VZ")]
        public DateTime? LhutaPrihlaseni { get; set; } = null;

        [Date()]
        [Description("Datum uzavření smlouvy u ukončené zakázky")]
        public DateTime? DatumUzavreniSmlouvy { get; set; } = null;


        [Date()]
        [Description("Poslední změna VZ podle poslední změny formuláře.")]
        public DateTime? PosledniZmena { get; set; } = null;


        [Description("Číselná hodnota odvozeného stavu zakázky z enumerace 'StavyZakazky'.")]
        public int StavVZ { get; set; } = 0;
        
        [Object(Ignore = true)]
        public StavyZakazky StavZakazky
        {
            get
            {
                return (StavyZakazky)StavVZ;
            }
            set
            {
                StavVZ = (int)value;
            }
        }



        public string FormattedCena(bool html)
        {
            if (KonecnaHodnotaBezDPH.HasValue)
            {
                return RenderData.NicePrice(KonecnaHodnotaBezDPH.Value, html: html);
            }
            else if (OdhadovanaHodnotaBezDPH.HasValue)
            {
                return RenderData.NicePrice(OdhadovanaHodnotaBezDPH.Value, html: html);
            }
            else
                return String.Empty;
        }

        public DateTime? GetPosledniZmena()
        {
            if (LastestFormular() != null)
            {
                return LastestFormular().Zverejnen;
            }
            else if (DatumUverejneni.HasValue)
            {
                return DatumUverejneni;
            }
            else
            {
                return null;
            }

        }


        [Description("")]
        public DateTime? LastUpdated { get; set; } = null;

        [Description("Odhadovaná hodnota zakázky bez DPH.")]
        [Number]
        public decimal? OdhadovanaHodnotaBezDPH { get; set; } = null;
        [Description("Konečná (vysoutěžená) hodnota zakázky bez DPH.")]
        [Number]
        public decimal? KonecnaHodnotaBezDPH { get; set; } = null;

        [Description("Měna odhadovaná hodnoty zakázky.")]
        [Keyword]
        public string OdhadovanaHodnotaMena { get; set; }
        [Description("Měna konečné hodnoty zakázky.")]
        [Keyword]
        public string KonecnaHodnotaMena { get; set; }

        [Description("Seznam známých url odkazů na umístění zakázky na webu.")]
        [Object()]
        public HashSet<string> UrlZakazky { get; set; } = new();

        [Description("Dokumenty příslušející zakázky (typicky zadávací a smluvní dokumentace)")]
        public class Document : IEquatable<Document>
        {
            [Description("Kontrolní součet SHA256 souboru pro ověření unikátnosti")]
            [Text]
            public string Sha256Checksum { get; set; }
            
            [Description("Velikost souboru v Bytech")]
            public long SizeInBytes { get; set; }

            [Description("URL odkazy uvedené v profilu zadavatele")]
            [Keyword]
            public HashSet<string> OficialUrls { get; set; } = new();
            
            [Description("Přímé URL na tento dokument.")]
            [Keyword]
            public HashSet<string> DirectUrls { get; set; } = new();
            
            [Description("Popis obsahu dokumentu, z XML na profilu z pole dokument/typ_dokumentu.")]
            [Keyword]
            public string TypDokumentu { get; set; }

            [Description("Datum vložení, uveřejnění dokumentu.")]
            [Date]
            public DateTime? VlozenoNaProfil { get; set; }
            
            [Description("Číslo verze")]
            [Keyword]
            public string CisloVerze { get; set; }

            [Description("Neuvádět, obsah dokumentu v plain textu pro ftx vyhledávání")]
            [Text()]
            public string PlainText { get; set; }
            
            [Description("Neuvádět.")]
            public DataQualityEnum PlainTextContentQuality { get; set; } = DataQualityEnum.Unknown;

            [Description("Neuvádět")]
            [Date]
            public DateTime? LastUpdate { get; set; } = null;

            [Description("Neuvádět")]
            [Date]
            public DateTime? LastProcessed { get; set; } =null;

            [Description("Neuvádět")]
            [Keyword()]
            public string ContentType { get; set; }
            [Description("Neuvádět")]
            public int Lenght { get; set; }
            [Description("Neuvádět")]
            public int WordCount { get; set; }
            [Description("Neuvádět")]
            public int Pages { get; set; }

            [Keyword()]
            public HashSet<string> StorageIds { get; set; } = new();

            [Keyword()]
            public HashSet<string> PlainDocumentIds { get; set; } = new();

            public string GetDocumentUrlToDownload()
            {
                if (StorageIds.Count == 0 || string.IsNullOrWhiteSpace(StorageIds.FirstOrDefault()))
                    return DirectUrls.FirstOrDefault();
                
                return $"http://bpapi.datlab.eu/document/{StorageIds.FirstOrDefault()}";
            }

            public string GetHlidacStorageId()
            {
                return Sha256Checksum + "_" + SizeInBytes;
            }

            [Keyword()]
            public string Name { get; set; }

            [Object(Ignore = true)]
            public bool EnoughExtractedText
            {
                get
                {
                    return !string.IsNullOrEmpty(PlainText) && (Lenght >= 20 || WordCount >= 10);
                }
            }

            public void CalculateDocStats()
            {
                Lenght = PlainText.Length;
                WordCount = Devmasters.TextUtil.CountWords(PlainText);
            }

            public bool Equals(Document other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                if (string.IsNullOrWhiteSpace(Sha256Checksum)) return false;

                return Sha256Checksum == other.Sha256Checksum; 
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Document)obj);
            }

        }

        [Description("Hodnotící kritéria veřejné zakázky")]
        public class HodnoticiKriteria : IEquatable<HodnoticiKriteria>
        {
            [Description("Pořadí.")]
            public int Poradi { get; set; }
            [Keyword]
            [Description("Popis kritéria")]
            public string Kriterium { get; set; }
            [Text]
            [Description("Název kritéria")]
            public string Nazev { get; set; }

            [Number]
            [Description("Váha v hodnocení")]
            public decimal Vaha { get; set; } = 0;

            public bool Equals(HodnoticiKriteria other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Poradi == other.Poradi &&
                       string.Equals(Kriterium, other.Kriterium, StringComparison.InvariantCultureIgnoreCase) &&
                       string.Equals(Nazev, other.Nazev, StringComparison.InvariantCultureIgnoreCase) &&
                       Vaha == other.Vaha;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((HodnoticiKriteria)obj);
            }

            public override int GetHashCode()
            {
                var hashCode = new HashCode();
                hashCode.Add(Poradi);
                hashCode.Add(Kriterium, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(Nazev, StringComparer.InvariantCultureIgnoreCase);
                hashCode.Add(Vaha);
                return hashCode.ToHashCode();
            }

            public static bool operator ==(HodnoticiKriteria left, HodnoticiKriteria right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(HodnoticiKriteria left, HodnoticiKriteria right)
            {
                return !Equals(left, right);
            }
        }

        public class Subject : IEquatable<Subject>
        {
            [Keyword()]
            [Description("IC subjektu")]
            public string ICO { get; set; }
            [Keyword()]
            [Description("Obchodní jméno")]
            public string Jmeno { get; set; }
            [Keyword()]
            [Description("Profil zadavatele")]
            public string ProfilZadavatele { get; set; }

            public bool Equals(Subject other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return string.Equals(ICO, other.ICO, StringComparison.InvariantCultureIgnoreCase);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((Subject)obj);
            }

            public override int GetHashCode()
            {
                return ICO.GetHashCode();
            }

            public static bool operator ==(Subject left, Subject right)
            {
                return Equals(left, right);
            }

            public static bool operator !=(Subject left, Subject right)
            {
                return !Equals(left, right);
            }
        }


        [Text(Index = false)]
        [Description("HTML stránky zakázky, pokud bylo parsováno z HTML")]
        public string RawHtml { get; set; }

        public static string CPVToText(string cpv)
        {
            if (string.IsNullOrEmpty(cpv))
                return "";
            if (Validators.CPVKody.ContainsKey(cpv))
                return Validators.CPVKody[cpv];

            if (cpv.Contains("-"))
            {
                int nalez = cpv.IndexOf("-");
                cpv = cpv.Substring(0, nalez);
            }
            var key = Validators.CPVKody.Keys.FirstOrDefault(m => m.StartsWith(cpv));
            if (key != null)
                return Validators.CPVKody[key];
            else
                return cpv;

        }

        public string GetUrl(bool local = true)
        {
            return GetUrl(local, string.Empty);
        }

        public string GetUrl(bool local, string foundWithQuery)
        {
            string url = "/verejnezakazky/zakazka/" + Id;// E49DE92B876B0C66C2F29457EB61C7B7

            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

            if (local == false)
                return "https://www.hlidacstatu.cz" + url;
            else
                return url;
        }

        public Formular LastestFormular()
        {
            if (Formulare != null)
                return Formulare.OrderByDescending(m => m.Zverejnen).FirstOrDefault();
            else
                return null;
        }

        static string cisloZakazkyRegex = @"Evidenční \s* číslo \s* zakázky: \s*(?<zakazka>(Z?)\d{1,8}([-]*\d{1,7})?)\s+";
        static string cisloConnectedFormRegex = @"Evidenční \s* číslo \s* souvisejícího \s* formuláře: \s*(?<zakazka>(F?)\d{1,8}([-]*\d{1,7})?)\s+";
        static string cisloFormRegex = @"Evidenční \s* číslo \s* formuláře: \s*(?<zakazka>(F?)\d{1,8}([-]*\d{1,7})?)\s+";
        static string nazevZakazkyRegex = @"Název \s* zakázky: \s*(?<nazev>.*)\s+<div";
        static string zadavatelNazev = @"Název \s* zadavatele: \s*(?<zadavatel>.*)\s+<div";
        static string zadavatelICO = @"IČO \s* zadavatele: \s*(?<zadavatel>\d{2,8})\s+<div";

        static string datumUverejneniRegex = @"Datum\s*uveřejnění\s*ve\s*VVZ:\s*(?<datum>[0-9.: ]*)\s*";

        static string[] formsToSkip = new string[] { "F07", "F08", "CZ05", "CZ06" };



        public static T GetElemVal<T>(XDocument xd, string name)
        {

            if (typeof(T) == typeof(string))
            {
                return (T)ParseTools.ChangeType(xd.Root.Element(name)?.Value, typeof(T));
            }
            else if (typeof(T) == typeof(decimal?))
            {
                return (T)ParseTools.ChangeType(ParseTools.ToDecimal(xd.Root.Element(name)?.Value), typeof(T));
            }
            else if (typeof(T) == typeof(int?))
            {
                if (xd.Root.Element(name) == null)
                    return default(T);
                else
                    return (T)ParseTools.ChangeType((int?)ParseTools.ToDecimal(xd.Root.Element(name)?.Value), typeof(T));
            }
            else if (typeof(T) == typeof(DateTime?))
            {
                if (xd.Root.Element(name) == null)
                    return default(T);
                else
                    return (T)ParseTools.ChangeType(Devmasters.DT.Util.ToDateTime(xd.Root.Element(name)?.Value, "yyyy-MM-ddThh:mm:ss", "dd.MM.yyyy"), typeof(T));
            }
            else
                throw new NotImplementedException();


        }


        public static bool IsElem(XDocument xd, string name)
        {
            return string.IsNullOrEmpty(xd.Root.Element(name).Value) == false;
        }

        public static VerejnaZakazka FromHtml(string html, int displayId)
        {
            throw new NotImplementedException("not finished yet. See XML parser");

        }

        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "Veřejná zakázka";
        }

        public string ToAuditObjectId()
        {
            return Id;
        }

        public string BookmarkName()
        {
            return NazevZakazky;
        }
        public bool NotInterestingToShow() { return false; }






        public class ZakazkaSource
        {
            public string ZakazkaURL { get; set; }
            public string ProfilZadavatelUrl { get; set; }
            public string SearchZakazkaUrl { get; set; }

        }
        public class ExportedVZ
        {
            public class SubjectExport
            {
                public SubjectExport() { }

                public string ICO { get; set; }
                public string Jmeno { get; set; }
                public string KrajId { get; set; }
                public string OkresId { get; set; }
            }
            public string Id { get; set; }
            public string EvidencniCisloZakazky { get; set; }
            public SubjectExport Zadavatel { get; set; }
            public Subject[] Dodavatele { get; set; }
            public string NazevZakazky { get; set; }
            public string PopisZakazky { get; set; }
            public string[] CPV { get; set; }
            public DateTime? DatumUverejneni { get; set; }
            public DateTime? LhutaDoruceni { get; set; }
            public DateTime? DatumUzavreniSmlouvy { get; set; }
            public DateTime? PosledniZmena { get; set; }
            public StavyZakazky StavZakazky { get; set; }
            public decimal? OdhadovanaHodnotaBezDPH { get; set; } = null;
            public decimal? KonecnaHodnotaBezDPH { get; set; } = null;
            public string OdhadovanaHodnotaMena { get; set; }
            public string KonecnaHodnotaMena { get; set; }
            public string UrlProfiluZadavatele { get; set; }
            public ZakazkaSource ZdrojZakazky { get; set; }
        }
        
        //todo: odstranit nepoužívané
        public VerejnaZakazka Export(bool allData = false)
        {
            VerejnaZakazka vz = (VerejnaZakazka)MemberwiseClone();
            if (allData == false)
            {
                if (vz.Dokumenty != null)
                {
                    foreach (var vzd in vz.Dokumenty)
                    {
                        vzd.DirectUrls = new HashSet<string>();
                        vzd.PlainDocumentIds = new HashSet<string>();
                        vzd.PlainText = "-- Tato data jsou dostupná pouze v komerční nebo speciální licenci. Kontaktujte nás. --";
                    }
                }
            }
            vz.RawHtml = "";
            return vz;
        }
//todo: dořešit exporty
        public ExpandoObject FlatExport()
        {
            dynamic v = new ExpandoObject();
            v.Url = GetUrl(false);
            v.datasety = Zdroje;

            v.PosledniZmena = PosledniZmena;
            v.LhutaDoruceni = LhutaDoruceni;
            v.KonecnaHodnotaBezDPH = KonecnaHodnotaBezDPH;
            v.OdhadovanaHodnotaBezDPH = OdhadovanaHodnotaBezDPH;
            v.NazevZakazky = Devmasters.TextUtil.NormalizeToBlockText(NazevZakazky);
            v.PopisZakazky = Devmasters.TextUtil.NormalizeToBlockText(PopisZakazky);
            v.ZadavatelJmeno = Zadavatel?.Jmeno;
            v.ZadavatelIco = Zadavatel?.ICO;

            for (int i = 0; i < Dodavatele?.Count(); i++)
            {
                ((IDictionary<String, Object>)v).Add($"DodavatelJmeno_{i}", Dodavatele[i].Jmeno);
                ((IDictionary<String, Object>)v).Add($"DodavatelIco_{i}", Dodavatele[i].ICO);
            }
            return v;
        }
    }
}
