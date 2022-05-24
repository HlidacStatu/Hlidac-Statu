using Nest;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Entities.Dotace
{
    [ElasticsearchType(IdProperty = nameof(IdDotace))]
    public partial class Dotace : IBookmarkable, IFlattenedExport
    {
        [Keyword]
        public string IdDotace { get; set; }
        [Date]
        public DateTime? DatumPodpisu { get; set; }
        [Date]
        public DateTime? DatumAktualizace { get; set; }
        [Keyword]
        public string KodProjektu { get; set; }
        [Text]
        public string NazevProjektu { get; set; }
        [Object]
        public Zdroj Zdroj { get; set; }

        [Object]
        public Prijemce Prijemce { get; set; }

        [Object]
        public DotacniProgram Program { get; set; }
        // calculated fields
        // Celková výše dotace včetně půjčky
        [Number]
        public decimal? DotaceCelkem { get; set; }
        // Půjčené peníze
        [Number]
        public decimal? PujckaCelkem { get; set; }

        // Rozhodnutí je neuvěřitelné hausnumero a může se v něm skrývat cokoliv.
        // Například to může být hodnota, která odpovídá nákladům na realizování dotovaného projektu společnosti, 
        // kde dotace samotná může dosahovat třeba jen 40 % z rozhodnuté částky
        [Object]
        public List<Rozhodnuti> Rozhodnuti { get; set; } = new List<Rozhodnuti>();
        [Keyword]
        public string Duplicita { get; set; }

        [Text]
        public List<string> Chyba { get; set; }


        public string BookmarkName() => GetNazevDotace();

        public string GetNazevDotace()
        {
            if (!string.IsNullOrWhiteSpace(NazevProjektu))
            {
                return NazevProjektu;
            }
            if (!string.IsNullOrWhiteSpace(KodProjektu))
            {
                return KodProjektu;
            }

            return "";
        }

        public string GetUrl(bool local = true) => GetUrl(local, string.Empty);

        public string GetUrl(bool local, string foundWithQuery)
        {
            string url = "/Dotace/Detail/" + IdDotace;
            if (!string.IsNullOrEmpty(foundWithQuery))
                url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

            if (local == false)
                return "https://www.hlidacstatu.cz" + url;
            else
                return url;
        }


        public string ToAuditJson() => Newtonsoft.Json.JsonConvert.SerializeObject(this);

        public string ToAuditObjectId() => IdDotace;

        public string ToAuditObjectTypeName() => "Dotace";

        public void CalculateTotals()
        {
            if (Rozhodnuti is null || Rozhodnuti.Count == 0)
            {
                DotaceCelkem = null;
                PujckaCelkem = null;
            }
            else
            {
                // we need to refresh values (in case something was changed)
                Rozhodnuti.ForEach(r => r.RecalculateCerpano());

                DotaceCelkem = Rozhodnuti.Sum(r => r.CerpanoCelkem ?? 0);
                PujckaCelkem = Rozhodnuti.Where(r => r.JePujcka.HasValue && r.JePujcka.Value).Sum(r => r.CerpanoCelkem ?? 0);
            }

        }

        public void CalculateCerpaniYears()
        {
            if (Rozhodnuti is null || Rozhodnuti.Count == 0)
            {
                Rozhodnuti = new List<Rozhodnuti>();
            }
            else
            {
                foreach (var rozhodnuti in Rozhodnuti)
                {
                    if (rozhodnuti.Cerpani is null)
                        continue;
                    foreach (var cerpani in rozhodnuti.Cerpani)
                    {
                        cerpani.GuessedYear = cerpani.Rok ?? rozhodnuti.Rok ?? DatumPodpisu?.Year;
                    }
                }
            }
        }

        public ExpandoObject FlatExport()
        {
            dynamic v = new ExpandoObject();
            v.Url = GetUrl(false);
            v.idDotace = IdDotace;
            v.prijmenceJmeno = Prijemce?.Jmeno;
            v.prijmenceIco = Prijemce?.Ico;
            v.prijmenceJmeno = Prijemce.Jmeno;
            v.prijmenceJmenoAktualni = Prijemce.HlidacJmeno;
            v.obec = Prijemce.Obec;
            v.nazevProjektu = GetNazevDotace();
            v.dotacniProgram = Program?.Kod ?? "neuveden";
            v.datumPodpisu = DatumPodpisu;
            v.dotace = DotaceCelkem;
            v.vratnaDotace = PujckaCelkem;

            return v;
        }
    }
}
