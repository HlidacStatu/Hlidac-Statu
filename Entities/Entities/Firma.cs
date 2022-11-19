using Devmasters;

using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Datastructures.Graphs2;

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;

namespace HlidacStatu.Entities
{
    public partial class Firma
        : IBookmarkable
    {
        public enum TypSubjektuEnum
        {
            Neznamy = -1,
            Soukromy = 0,
            PatrimStatu = 1,
            PatrimStatu25perc = 2,
            Ovm = 10,
            Obec = 20
        }

        public static bool IsValid(Firma f)
        {
            if (f == null)
                return false;
            else
                return f.Valid;
        }

        public static Firma NotFound = new Firma() { ICO = "notfound", Valid = false };
        public static Firma LoadError = new Firma() { ICO = "error", Valid = false };


        static string cnnStr = Config.GetWebConfigValue("OldEFSqlConnection");


        //vcetne strategickych podniku z https://www.mfcr.cz/cs/aktualne/tiskove-zpravy/2020/vlada-schvalila-strategii-vlastnicke-pol-37573
        public static string[] StatniFirmyICO = new string[]
        {
            "00000205","00000337","00000345","00000370","00000493","00000515","00001279","00001481","00001490","00002321","00002674","00002691",
            "00002739","00003026","00007536","00008141","00008184","00009181","00009334","00009393","00009563","00010669","00011011","00012033",
            "00012343","00013251","00013455","00014079","00014125","00014818","00015156","00015270","00015296","00015415","00015679","00024007",
            "00076791","00086932","00128201","00157287","00157325","00251976","00311391","00514152","00565253","00577880","00659819","00664073",
            "03630919","13695673","14450216","14450241","14867770","14888025","15503852","17047234","24272523","24729035","24821993","24829871",
            "25059386","25085531","25125877","25255843","25291581","25401726","25634160","25674285","25702556","25938924","26051818","26162539",
            "26175291","26206803","26376547","26463318","26470411","26840065","26871823","27145573","27146235","27195872","27232433","27257258",
            "27257517","27309941","27364976","27378225","27772683","27786331","27804721","27892646","28196678","28244532","28255933","28267141",
            "28707052","28786009","28861736","29372259","42196451","43833560","44269595","44848943","45144419","45147965","45193070","45273375",
            "45273448","45274649","45274827","45279314","45534268","45795908","46355901","46504818","46708707","47114983","47115726","47673354",
            "47677543","48204285","48291749","48535591","49241494","49241672","49453866","49454561","49710371","49901982","49973720","60193468",
            "60193531","60196696","60197901","60698101","61459445","61860336","62413376","63078333","63080249","70889953","70889988","70890005",
            "70890013","70890021","70994226","70994234"
        };

        public static int[] StatniFirmy_BasedKodPF = new int[]
        {
            301, 302, 312, 313, 314, 325, 331, 352, 353, 361, 362, 381, 382, 521, 771, 801, 804, 805
        };

        /*
         * https://wwwinfo.mfcr.cz/ares/aresPrFor.html.cz
         * KOD_PF
         * 
301	Státní podnik
302	Národní podnik
312	Banka-státní peněžní ústav
313	Česká národní banka
314	Česká konsolidační agentura
325	Organizační složka státu
331	Příspěvková organizace
352	Správa železniční dopravní cesty, státní organizace
353	Rada pro veřejný dohled nad auditem
361	Veřejnoprávní instituce (ČT,ČRo,ČTK)
362	Česká tisková kancelář
381	Fond (ze zákona)
382	Státní fond ze zákona
 
521	Samostatná drobná provozovna obecního úřadu
771	Svazek obcí
801	Obec nebo městská část hlavního města Prahy
804	Kraj
805	Regionální rada regionu soudržnosti    

 */

        [Key]
        public string ICO { get; set; }
        public string DIC { get; set; }
        [NotMapped]
        public string[] DatovaSchranka { get; set; } = new string[] { };
        public DateTime? Datum_Zapisu_OR { get; set; }
        public byte? Stav_subjektu { get; set; }
        public int? Status { get; set; }
        public int? Typ { get; set; }

        public string ESA2010 { get; set; }

        public string KrajId { get; set; }
        public string OkresId { get; set; }

        public short? IsInRS { get; set; }

        public int? Kod_PF { get; set; }

        // https://wwwinfo.mfcr.cz/ares/nace/ares_nace.html.cz
        [NotMapped]
        public string[] NACE { get; set; } = new string[] { };
        public int VersionUpdate { get; set; }

        public string Source { get; set; }
        public string Popis { get; set; }
        private string _jmeno = string.Empty;

        public string Jmeno
        {
            get { return _jmeno; }
            set
            {
                _jmeno = value;
                JmenoAscii = TextUtil.RemoveDiacritics(value);
            }
        }

        public string JmenoAscii { get; set; }

        public int PocetZam { get; set; }
        public string KodOkresu { get; set; }
        public string ICZUJ { get; set; }
        public string KODADM { get; set; }
        public string Adresa { get; set; }
        public string PSC { get; set; }
        public string Obec { get; set; }
        public string CastObce { get; set; }
        public string Ulice { get; set; }
        public string CisloDomu { get; set; }
        public string CisloOrientacni { get; set; }



        [NotMapped]
        public TypSubjektuEnum TypSubjektu
        {
            get
            {
                if (this.Typ.HasValue)
                    return (TypSubjektuEnum)this.Typ;
                else
                    return TypSubjektuEnum.Neznamy;
            }
            set
            {
                this.Typ = (int)value;
            }
        }


        public string StatusFull(bool shortText = false)
        {
            return StatusFull(this.Status, shortText);
        }
        public static string StatusFull(int? status, bool shortText = false)
        {
            switch (status)
            {
                case 1:
                    return shortText ? "" : "";
                //Subjekt bez omezení v činnosti
                case 2:
                    return shortText ? "Subjekt v likvidaci" : "v likvidaci";
                case 3:
                    return shortText ? "Subjekt v insolvenčním řízení" : "v insolvenci";
                case 4:
                    return shortText ? "Subjekt v likvidaci a v insolvenčním řízení" : "v likvidaci";
                case 5:
                    return shortText ? "Subjekt v nucené správě" : "v nucené správě";
                case 6:
                    return shortText ? "Zaniklý subjekt" : "zaniklý subjekt";
                case 7:
                    return shortText ? "Subjekt s pozastavenou, přerušenou činností" : "pozastavená činností";
                case 8:
                    return shortText ? "Dosud nezahájil činnost" : "nezahájená činnost";

                default:
                    return "";
            }
        }

        public string JmenoOrderReady()
        {
            string[] prefixes = new string[]
                {"^Statutární\\s*město\\s", "^Město\\s ", "^Městská\\s*část\\s ", "^Obec\\s "};
            string jmeno = Jmeno.Trim();
            foreach (var pref in prefixes)
            {
                if (Regex.IsMatch(jmeno, pref,
                    RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
                    RegexOptions.CultureInvariant))
                    return jmeno.ReplaceWithRegex("", pref).Trim();
            }

            return Jmeno;
        }


        // https://wwwinfo.mfcr.cz/ares/aresPrFor.html.cz



        //External.RPP.KategorieOVM[] _kategorieOVM = null;

        public Graph.Edge[] _vazby = null;
        public Graph.Edge[] _parentVazbyFirmy = null;
        public Firma[] _parents = null;

        public Graph.Edge[] _parentVazbyOsoby = null;

        //Napojení na graf
        public UnweightedGraph _graph = null;
        public Vertex<string> _startingVertex = null; //not for other use except as a search starting point

        bool? _valid = null;


        [NotMapped]
        public bool Valid
        {
            get
            {
                if (_valid == null)
                    _valid = !(Jmeno == NotFound.Jmeno
                               || Jmeno == LoadError.Jmeno);

                return _valid.Value;
            }

            private set { _valid = value; }
        }


        public string JmenoBezKoncovky()
        {
            return JmenoBezKoncovky(Jmeno);
        }

        public string KoncovkaFirmy()
        {
            string koncovka;
            JmenoBezKoncovkyFull(Jmeno, out koncovka);
            return koncovka;
        }


        public static string[] Koncovky = new string[]
        {
            "a.s.",
            "a. s.",
            "akciová společnost",
            "akc. společnost",
            "s.r.o.", "s r.o.", "s. r. o.",
            "spol. s r.o.", "spol.s r.o.", "spol.s.r.o.", "spol. s r. o.",
            "v.o.s.", "v. o. s.",
            "veřejná obchodní společnost",
            "s.p.", "s. p.",
            "státní podnik",
            "odštepný závod",
            "o.z.", "o. z.",
            "o.s.", "o. s.",
            "z.s.", "z. s.",
            "z.ú.", "z. ú.",
        };


        public static string JmenoBezKoncovky(string name)
        {
            string ret;
            return JmenoBezKoncovkyFull(name, out ret);
        }

        public static string JmenoBezKoncovkyFull(string name, out string koncovka)
        {
            koncovka = string.Empty;
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string modifNazev = name.Trim().Replace((char)160, ' ');
            foreach (var k in Koncovky.OrderByDescending(m => m.Length))
            {
                if (modifNazev.EndsWith(k))
                {
                    modifNazev = modifNazev.Replace(k, "").Trim();
                    koncovka = k;
                    break;
                }

                if (k.EndsWith("."))
                {
                    if (modifNazev.EndsWith(k.Substring(0, k.Length - 1)))
                    {
                        modifNazev = modifNazev.Replace(k.Substring(0, k.Length - 1), "").Trim();
                        koncovka = k;
                        break;
                    }
                }
            }

            if (modifNazev.EndsWith(",") || modifNazev.EndsWith(",") || modifNazev.EndsWith(";"))
                modifNazev = modifNazev.Substring(0, modifNazev.Length - 1);

            return modifNazev.Trim();
        }


        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "Subjekt";
        }

        public string ToAuditObjectId()
        {
            return ICO;
        }

        public string GetUrl(bool local = true)
        {
            return GetUrl(local, string.Empty);
        }

        public string GetUrl(bool local, string foundWithQuery)
        {
            return GetUrl(this.ICO,local,foundWithQuery);
        }
        public static string GetUrl(string ico, bool local, string foundWithQuery = "")
        {
            string url = "/subjekt/" + ico;
            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);
            if (!local)
                url = "https://www.hlidacstatu.cz" + url;

            return url;
        }

        public string BookmarkName()
        {
            return Jmeno;
        }
    }
}