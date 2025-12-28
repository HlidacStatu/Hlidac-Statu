using Devmasters;
using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using HlidacStatu.Util;
using Devmasters.Enums;
using System.Diagnostics.CodeAnalysis;

namespace HlidacStatu.Entities
{
    public partial class Firma
        : IBookmarkable
    {
        [ShowNiceDisplayName()]
        public enum TypSubjektuEnum
        {
            [NiceDisplayName("Neznámý")]
            Neznamy = -1,
            [NiceDisplayName("Soukromý")]
            Soukromy = 0,

            [NiceDisplayName("Insolvenční správce")]
            InsolvecniSpravce = 1,
            [NiceDisplayName("Exekutor")]
            Exekutor = 2,

            [NiceDisplayName("Státní")]
            PatrimStatu = 9,
            [NiceDisplayName("Úřad")]
            Ovm = 10,
            [NiceDisplayName("Obec")]
            Obec = 20
        }

        public static Func<Firma,bool> SoukromySubjektPredicate => (f)=>f.Typ <= (int)TypSubjektuEnum.Soukromy;
        public static Func<Firma, bool> StatniFirmaPredicate => (f) => (f.Typ == (int)TypSubjektuEnum.PatrimStatu);
        public static Func<Firma, bool> PatrimStatuPredicate => (f) => (f.Typ >= (int)TypSubjektuEnum.PatrimStatu );
        public static Func<Firma, bool> OVMSubjektPredicate => (f) => (f.Typ >= (int)TypSubjektuEnum.Ovm);


        public static string TypSubjektuDescription(int? typ, int pad, bool jednotne = true)
        {
            if (typ.HasValue)
                return TypSubjektuDescription((TypSubjektuEnum)typ, pad, jednotne);
            else
                return "";
        }
        public static string TypSubjektuDescription(TypSubjektuEnum typ, int pad, bool jednotne = true)
        {
            if (jednotne)
            {
                switch (typ)
                {
                    case TypSubjektuEnum.Neznamy:
                        if (pad == 4)
                            return "neznámou firmu";
                        else
                            return "neznámá firma";
                    case TypSubjektuEnum.Soukromy:
                        if (pad == 4)
                            return "soukromou firmu";
                        else
                            return "soukromá firma";
                    case TypSubjektuEnum.PatrimStatu:
                        if (pad == 4)
                            return "firmu vlastněnou státem";
                        else
                            return "firma vlastněná státem";
                    //case TypSubjektuEnum.PatrimStatuAlespon25perc:
                    //    if (pad == 4)
                    //        return "firmu vlastněnou státem (podíl min 25%)";
                    //    else
                    //        return "firma vlastněná státem (podíl min 25%)";
                    case TypSubjektuEnum.Ovm:
                        if (pad == 4)
                            return "úřad";
                        else
                            return "uřad";
                    case TypSubjektuEnum.Obec:
                        if (pad == 4)
                            return "obec";
                        else
                            return "obec";
                    default:
                        return "";
                }
            }
            else
            {

                switch (typ)
                {
                    case TypSubjektuEnum.Neznamy:
                        if (pad == 4)
                            return "neznámé firmy";
                        else
                            return "neznámé firmy";
                    case TypSubjektuEnum.Soukromy:
                        if (pad == 4)
                            return "soukromé firmy";
                        else
                            return "soukromé firmý";
                    case TypSubjektuEnum.PatrimStatu:
                        if (pad == 4)
                            return "firmy vlastněné státem";
                        else
                            return "firma vlastněná státem";
                    //case TypSubjektuEnum.PatrimStatuAlespon25perc:
                    //    if (pad == 4)
                    //        return "firmy vlastněné státem (podíl min 25%)";
                    //    else
                    //        return "firmy vlastněné státem (podíl min 25%)";
                    case TypSubjektuEnum.Ovm:
                        if (pad == 4)
                            return "úřady";
                        else
                            return "uřady";
                    case TypSubjektuEnum.Obec:
                        if (pad == 4)
                            return "obce";
                        else
                            return "obce";
                    default:
                        return "";
                }
            }
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


        [Key]
        public string ICO { get; set; }

        public string DIC { get; set; }

        [NotMapped]
        public string[] DatovaSchranka { get; set; } = new string[] { };

        public DateTime? Datum_Zapisu_OR { get; set; }
        public DateTime? DatumZaniku { get; set; }
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
            get {
                if (string.IsNullOrEmpty(_jmeno))
                {
                    return this.ICO;
                }
                return _jmeno;

            }
            set
            {
                _jmeno = value;
                JmenoAscii = TextUtil.RemoveDiacritics(value);

            }
        }

        public string JmenoAscii { get; set; }

        public int? PocetZamKod { get; set; }
        public string? IndustryKod { get; set; }
        public int? ObratKod { get; set; } = null;
        public int? PlatceDPHKod { get; set; } = null;
        public decimal? CompanyIndexKod { get; set; } = null;
        public int? Je_nespolehlivym_platcem_DPHKod { get; set; } = null;
        public int? Ma_dluh_vzpKod { get; set; } = null;


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
            set { this.Typ = (int)value; }
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
                { "^Statutární\\s*město\\s", "^Město\\s ", "^Městská\\s*část\\s ", "^Obec\\s " };
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

        public Graph.Edge[] _vazbyUredni = null;
        public Graph.Edge[] _vazby = null;
        public Graph.Edge[] _parentVazbyFirmy = null;
        public Firma[] _parents = null;

        public Graph.Edge[] _parentVazbyOsoby = null;

        //Napojení na graf
        public Dictionary<Relation.CharakterVazbyEnum, UnweightedGraph> _graphs = new ();
        public Dictionary<Relation.CharakterVazbyEnum, Vertex<string>> _graphStartingVertexs = new ();

        public UnweightedGraph _getGraph(Relation.CharakterVazbyEnum charakter)
        {
            if (_graphs.ContainsKey(charakter))
                return _graphs[charakter];
            else
                return null;
        }
        public void _setGraph(Relation.CharakterVazbyEnum charakter, UnweightedGraph graph)
        {
               _graphs[charakter] = graph;
        }

        public Vertex<string> _getStartingVertext(Relation.CharakterVazbyEnum charakter)
        {
            if (_graphStartingVertexs.ContainsKey(charakter))
                return _graphStartingVertexs[charakter];
            else
                return null;
        }
        public void _setStartingVertext(Relation.CharakterVazbyEnum charakter, Vertex<string> vertex)
        {
            _graphStartingVertexs[charakter] = vertex;
        }

        bool? _valid = null;


        [NotMapped]
        public bool Valid
        {
            get
            {
                if (_valid == null)
                    _valid = !(ICO == NotFound.ICO
                               || ICO == LoadError.ICO
                               || string.IsNullOrEmpty(this.Jmeno)
                               || string.IsNullOrWhiteSpace(this.ICO)
                               );

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


        private static readonly Dictionary<string, List<string>> _koncovky = new()
        {
            ["s.r.o."] = new()
            {
                "s.r.o.",
                "s r.o.",
                "s. r. o.",
                "spol. s r.o.",
                "spol.s r.o.",
                "spol.s.r.o.",
                "spol. s r. o.",
            },
            ["a.s."] = new()
            {
                "a.s.",
                "a. s.",
                "akciová společnost",
                "akc. společnost",
            },
            ["v.o.s."] = new()
            {
                "v.o.s.",
                "v. o. s.",
                "veřejná obchodní společnost",
            },
            ["s.p."] = new()
            {
                "s.p.",
                "s. p.",
                "státní podnik",
            },
            ["o.z."] = new()
            {
                "odštepný závod",
                "o.z.",
                "o. z.",
            },
            ["o.s."] = new()
            {
                "o.s.",
                "o. s.",
            },
            ["z.s."] = new()
            {
                "z.s.",
                "z. s.",
            },
            ["z.ú."] = new()
            {
                "z.ú.",
                "z. ú.",
            },
        };

        public static string[] Koncovky = _koncovky
            .SelectMany(k => k.Value)
            .Select(m => m.Trim().ToLower())
            .Distinct()
            .ToArray();
        public static string[] KoncovkyAscii = _koncovky
            .SelectMany(k =>  k.Value)
            .Select(m=> Devmasters.TextUtil.RemoveAccents(m).Trim().ToLower())
            .Distinct()
            .ToArray();


        public static string NormalizedKoncovka(string koncovka)
        {
            return _koncovky.FirstOrDefault(kvp => kvp.Value.Contains(koncovka)).Key;
        }
        
        public static string JmenoBezKoncovky(string name)
        {
            string ret;
            return JmenoBezKoncovkyFull(name, out ret);
        }
        public static string JmenoBezKoncovkyAsciiFull(string name, out string koncovka)
        {
            return _jmenoBezKoncovkyFull(KoncovkyAscii, name, out koncovka);
        }
        public static string JmenoBezKoncovkyFull(string name, out string koncovka)
        {
            return _jmenoBezKoncovkyFull(Koncovky,name, out koncovka);
        }
        private static string _jmenoBezKoncovkyFull(string[] koncovky, string name, out string koncovka)
        {
            koncovka = string.Empty;
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string modifNazev = name.Trim().Replace((char)160, ' ');
            foreach (var k in koncovky.OrderByDescending(m => m.Length))
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
            return GetUrl(this.ICO, local, foundWithQuery);
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

        public string Kraj()
        {
            if (KrajId is null)
                return "neznámý kraj";

            return CZ_Nuts.Kraje.TryGetValue(KrajId, out string kraj) ? kraj : KrajId;
        }

        public bool Registrovana_v_zahranici
        {
            get { return DataValidators.IsFirmaIcoZahranicni(this.ICO); }
        }

        public string BookmarkName()
        {
            return Jmeno;
        }
        }

    public  class FirmaByIcoComparer : IEqualityComparer<Firma>
    {
        public bool Equals(Firma x, Firma y)
        {
            if (x == null || y == null)
                return false;
            if (x.Valid == false || y.Valid == false)
                return false;
            return x.ICO == y.ICO;
        }

        public int GetHashCode([DisallowNull] Firma obj)
        {
            return obj.ICO.GetHashCode();
        }
    }
}