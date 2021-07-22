using Devmasters;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Datastructures.Graphs2;

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


        public static string[] StatniFirmyICO = new string[]
        {
            "45274649", "25702556", "70994226", "47114983", "42196451", "60193531", "00002739", "00514152", "00000493",
            "00001279", "24821993", "00000205", "60193468", "00000515", "49710371", "70890013", "70889953", "70890005",
            "25291581", "70889988", "70890021", "00007536", "26463318", "00024007", "25401726", "00015679", "00010669",
            "49241494", "14450216", "00001481", "00001490", "00002674", "43833560", "48204285", "00013251", "00014125",
            "27146235", "49973720", "00311391", "25125877", "00013455", "60197901", "60196696", "00251976", "62413376",
            "00577880", "44848943", "63078333", "45279314", "13695673", "27772683", "45273448", "28196678", "27786331",
            "61459445", "27364976", "24829871", "27257258", "17047234", "27378225", "27892646", "27195872", "45795908",
            "28244532", "61860336", "27145573", "25674285", "25085531", "27232433", "24729035", "27257517", "49901982",
            "27309941", "28786009", "47115726", "26871823", "26470411", "26206803", "28255933", "28707052", "26376547",
            "60698101", "27804721", "26840065", "25938924", "00128201", "26051818", "28861736"
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
        public int? Stav_subjektu { get; set; }
        public int? Status { get; set; }
        public int? Typ { get; set; }

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
            switch (Status)
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

        public string KrajId { get; set; }
        public string OkresId { get; set; }

        public short? IsInRS { get; set; }

        //migrace: ošklivej hack
        public FirmaHint _firmaHint = null;

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

        public int? Kod_PF { get; set; }

        // https://wwwinfo.mfcr.cz/ares/nace/ares_nace.html.cz
        [NotMapped]
        public string[] NACE { get; set; } = new string[] { };
        public int VersionUpdate { get; set; }

        public string Source { get; set; }
        public string Popis { get; set; }


        //External.RPP.KategorieOVM[] _kategorieOVM = null;

        public Graph.Edge[] _vazby = null;
        public Graph.Edge[] _parentVazbyFirmy = null;
        public Firma[] _parents = null;

        public Graph.Edge[] _parentVazbyOsoby = null;

        //Napojení na graf
        public UnweightedGraph _graph = null;
        public Vertex<string> _startingVertex = null; //not for other use except as a search starting point

        bool? _valid = null;

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
            string url = "/subjekt/" + ICO;
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