using Devmasters.Enums;

using HlidacStatu.DS.Graphs;
using HlidacStatu.DS.Graphs2;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace HlidacStatu.Entities
{
    [MetadataType(typeof(OsobaMetadata))] // napojení na metadata (validace)
    public partial class Osoba
        : IBookmarkable
    {

        public static string[] TitulyPred = new string[] {
            "Akad. mal.","Akad. malíř","arch.","as.","Bc.","Bc. et Bc.","BcA.","Dip Mgmt.",
            "DiS.","Doc.","Dott.","Dr.","DrSc.","et","Ing.","JUDr.","Mag.",
            "MDDr.","Mg.","Mg.A.","MgA.","Mgr.","MSc.","MSDr.","MUDr.","MVDr.",
            "Odb. as.","PaedDr.","Ph.Dr.","PharmDr.","PhDr.","PhD.","PhMr.","prof.",
            "Prof.","RCDr.","RNDr.","RSDr.","RTDr.","ThDr.","ThLic.","ThMgr." };

        public static string[] TitulyPo = new string[] {
            "BA","HONS", "BBA",
            "DBA","DBA.","CertHE","DipHE","BSc","BSBA","BTh","MIM","BBS","DiM","Di.M.",
            "CSc.", "D.E.A.", "DiS.", "Dr.h.c.", "DrSc.", "FACP", "jr.", "LL.M.",
            "MBA", "MD", "MEconSc.", "MgA.", "MIM", "MPA", "MPH", "MSc.", "Ph.D.", "Th.D." };


        public Osoba()
        {
        }

        [ShowNiceDisplayName]
        public enum PhotoTypes
        {
            [NiceDisplayName("small.jpg")]
            Small,
            [NiceDisplayName("original.jpg")]
            Original,
            [NiceDisplayName("small.nobackground.jpg")]
            NoBackground,
            [NiceDisplayName("small.blackwhite.jpg")]
            BlackWhite,
            [NiceDisplayName("small.cartoon.jpg")]
            Cartoon,
            [NiceDisplayName("original.uploaded.jpg")]
            UploadedOriginal,
            [NiceDisplayName("small.uploaded.jpg")]
            UploadedSmall,
            [NiceDisplayName("source.txt")]
            SourceOfPhoto,
        }


        public StatusOsobyEnum StatusOsoby() { return (StatusOsobyEnum)Status; }

        [ShowNiceDisplayName()]
        public enum StatusOsobyEnum
        {
            [NiceDisplayName("Bez vazby na politiku")]
            NeniPolitik = 0,

            [NiceDisplayName("Bývalý politik")]
            ByvalyPolitik = 1,
            [NiceDisplayName("Osoba s vazbami na politiky")]
            VazbyNaPolitiky = 2,
            [NiceDisplayName("Politik")]
            Politik = 3,
            [NiceDisplayName("Sponzor polit. strany")]
            Sponzor = 4,
            [NiceDisplayName("Vysoký státní úředník")]
            VysokyUrednik = 5,
            [NiceDisplayName("Duplicita")]
            Duplicita = 22,
        }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Jmeno) && !string.IsNullOrEmpty(Prijmeni) && InternalId > 0;
        }

        public override string ToString()
        {
            //return base.ToString();
            return FullNameWithNarozeni();
        }
        public string FullNameBezTitulu(bool html = false)
        {
            string ret = string.Format("{1} {2}", Jmeno, Prijmeni).Trim();
            if (html)
                ret = ret.Replace(" ", "&nbsp;");
            return ret;
        }

        public string FullName(bool html = false)
        {
            string ret = string.Format("{0} {1} {2}{3}", TitulPred?.Trim(), Jmeno?.Trim(), Prijmeni?.Trim(), string.IsNullOrWhiteSpace(TitulPo) ? "" : ", " + TitulPo?.Trim()).Trim();
            if (html)
                ret = ret.Replace(" ", "&nbsp;");
            return ret;
        }
        public string PrijmeniJmeno(bool html = false)
        {
            string ret = $"{this.Prijmeni} {this.Jmeno}".Trim();
            if (html)
                ret = ret.Replace(" ", "&nbsp;");
            return ret;
        }

        public string FullNameWithYear(bool html = false)
        {
            var s = FullName(html);
            if (Narozeni.HasValue)
                s = s + NarozeniYear(html);
            if (html)
                s = s.Replace(" ", "&nbsp;");
            return s;
        }

        public string ShortName()
        {
            var f = Jmeno.FirstOrDefault();
            if (f == default(char))
                return Prijmeni;
            else
                return f + ". " + Prijmeni;
        }

        public string Inicialy()
        {
            return Jmeno.FirstOrDefault() + "" + Prijmeni.FirstOrDefault();
        }
        public string FullNameWithNarozeni(bool html = false)
        {
            var s = FullName(html);
            if (Narozeni.HasValue)
            {
                s = s + string.Format(" ({0}) ", Narozeni.Value.ToShortDateString());
            }
            if (html)
                s = s.Replace(" ", "&nbsp;");
            return s;
        }

        public string PohlaviCalculated()
        {
            var sex = Devmasters.Lang.CS.SexFromName.GetSex(this.Jmeno.Trim(),this.Prijmeni.Trim());
            switch (sex)
            {
                case Devmasters.Lang.CS.Vokativ.SexEnum.Woman:
                    return "f";
                case Devmasters.Lang.CS.Vokativ.SexEnum.Man:
                    return "m";
                case Devmasters.Lang.CS.Vokativ.SexEnum.Unknown:
                default:
                    return "";
            }
        }



        public static int[] VerejnopravniUdalosti = new int[] {
            (int)OsobaEvent.Types.VolenaFunkce,
            (int)OsobaEvent.Types.PolitickaExekutivni,
            (int)OsobaEvent.Types.Politicka,
            (int)OsobaEvent.Types.VerejnaSpravaJine,
            (int)OsobaEvent.Types.VerejnaSpravaExekutivni,
            (int)OsobaEvent.Types.Osobni,
            (int)OsobaEvent.Types.Jine
        };


        //migrace: ugly hack - tohle je hezké místo, co by šlo vylepšit
        public Dictionary<Relation.CharakterVazbyEnum, UnweightedGraph> _graphs = new();
        public Dictionary<Relation.CharakterVazbyEnum, Vertex2<string>> _graphStartingVertexs = new();

        public Graph.Edge[] _vazby = null;
        public Graph.Edge[] _vazbyUredni = null;

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

        public Vertex2<string> _getStartingVertext(Relation.CharakterVazbyEnum charakter)
        {
            if (_graphStartingVertexs.ContainsKey(charakter))
                return _graphStartingVertexs[charakter];
            else
                return null;
        }
        public void _setStartingVertext(Relation.CharakterVazbyEnum charakter, Vertex2<string> vertex)
        {
            _graphStartingVertexs[charakter] = vertex;
        }



        public string NarozeniYear(bool html = false)
        {
            string result = "";
            if (Narozeni.HasValue || Umrti.HasValue)
            {
                string narozeni = Narozeni?.ToString("*yyyy") ?? "* ???";
                string umrti = Umrti?.ToString(" - ✝yyyy") ?? "";
                result = $" ({narozeni}{umrti})";
            }
            if (html)
                result = result.Replace(" ", "&nbsp;");
            return result;
        }

        public string NarozeniUmrtiFull(bool html = false)
        {
            string result = "";
            if (Narozeni.HasValue || Umrti.HasValue)
            {
                string narozeni = Narozeni?.ToString("*dd.MM.yyyy") ?? "* ???";
                string umrti = Umrti?.ToString("- ✝dd.MM.yyyy") ?? "";
                result = $" ({narozeni} {umrti})";
            }
            if (html)
                result = result.Replace(" ", "&nbsp;");
            return result;
        }





        public Osoba ShallowCopy()
        {
            return (Osoba)MemberwiseClone();
        }

        public static string Capitalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            //return Regex.Replace(s, @"\b(\w)", m => m.Value.ToUpper());
            return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s);

        }

        public static string NormalizeJmeno(string s)
        {
            return Capitalize(s?.Trim());
        }
        public static string NormalizePrijmeni(string s)
        {
            return Capitalize(s?.Trim());
        }

        public static string NormalizeTitul(string s, bool pred)
        {
            return s?.Trim();
        }


        static System.Text.RegularExpressions.RegexOptions defaultRegexOptions = ((System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace | System.Text.RegularExpressions.RegexOptions.Multiline)
| System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        public static string JmenoBezTitulu(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string modifNazev = name.Trim().ToLower();
            foreach (var k in TitulyPo.Concat(TitulyPred))
            {
                string kRegex = @"(\b|^)" + System.Text.RegularExpressions.Regex.Escape(k) + @"(\b|$)";


                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(modifNazev, kRegex, defaultRegexOptions);
                if (m.Success)
                    modifNazev = System.Text.RegularExpressions.Regex.Replace(modifNazev, kRegex, "", defaultRegexOptions);

            }
            modifNazev = System.Text.RegularExpressions.Regex
                .Replace(modifNazev, "[^a-z-. ěščřžýáíéůúďňťĚŠČŘŽÝÁÍÉŮÚĎŇŤÄäÖöÜüßÀàÂâÆæÇçÈèÊêËëÎîÏïÔôŒœÙùÛûÜüŸÿ]", "", defaultRegexOptions)
                .Trim();


            return Capitalize(modifNazev);

        }
        public bool Zena()
        {
            if (string.IsNullOrEmpty(Pohlavi))
                Pohlavi = PohlaviCalculated();

            return Pohlavi == "f";
        }

        public bool Muz()
        {
            if (string.IsNullOrEmpty(Pohlavi))
                Pohlavi = PohlaviCalculated();

            return Pohlavi == "m";
        }

        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectTypeName()
        {
            return "Osoba";
        }

        public string ToAuditObjectId()
        {
            return InternalId.ToString();
        }

        public string GetUrl(bool local = true)
        {
            return GetUrl(local, string.Empty);
        }

        public string GetUrl(bool local, string foundWithQuery)
        {

            if (string.IsNullOrEmpty(NameId))
                return "";

            string url = "/osoba/" + NameId;
            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

            if (local == false)
                url = "https://www.hlidacstatu.cz" + url;

            return url;
        }

        public string BookmarkName()
        {
            return FullNameWithYear();
        }



        public static string AppendTitle(string titulPred, string titulPo)
        {
            var check = (titulPred + titulPo).Trim();
            if (string.IsNullOrWhiteSpace(check))
                return "";

            var sb = new StringBuilder();
            sb.Append(" (");
            sb.Append(titulPred);
            sb.Append(" ");
            sb.Append(titulPo);
            sb.Append(")");

            return sb.ToString();
        }

    }
}
