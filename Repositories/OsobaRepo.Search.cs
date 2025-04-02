using Devmasters;
using Devmasters.Enums;

using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Searching;
using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static HlidacStatu.Connectors.External.RZP;
using static HlidacStatu.Entities.Osoba;

namespace HlidacStatu.Repositories
{
    public static partial class OsobaRepo
    {


        public static class Searching
        {
            public static readonly Regex DateRegex = new(@"(\d{1,2}[-./\\]\d{1,2}[-./\\]\d{2,4})|(\d{2,4}[-./\\]\d{1,2}[-./\\]\d{1,2})");
            public static readonly Regex YearRegexLoose = new(@"(19|20)\d{2}");

            public const int DefaultPageSize = 40;
            public const int MaxResultWindow = 200;


            [ShowNiceDisplayName()]
            [Sortable(SortableAttribute.SortAlgorithm.BySortValue)]
            public enum OrderResult
            {
                [SortValue(0)]
                [NiceDisplayName("podle relevance")]
                Relevance = 0,


                [SortValue(1)]
                [NiceDisplayName("podle abecedy")]
                NameAsc = 1,


                [Disabled] FastestForScroll = 666
            }

            public static IRule[] irules = new IRule[]
            {
                new TransformPrefix("osobaid:", "osobaid:", null),
            };


            /// <summary>
            /// <summary>
            /// Performs a simple search for an Osoba based on the provided query, page number, page size, and sorting order.
            /// </summary>
            /// <param name="query">The search query string.</param>
            /// <param name="page">The page number for pagination.</param>
            /// <param name="pageSize">The number of results per page.</param>
            /// <param name="order">The order in which to sort the results.</param>
            /// <param name="exactNumOfResults">Indicates whether to return an exact number of results.</param>
            /// <returns>A task that represents the asynchronous operation, containing the search result.</returns>
            public static QueryContainer GetSimpleQuery(string query)
            {
                var qc = SimpleQueryCreator.GetSimpleQuery<Smlouva>(query, irules);
                return qc;
            }

            /// <summary>
            /// Performs a simple search for an Osoba based on the provided query, page number, page size, and sorting order.
            /// </summary>
            /// <param name="query">The search query string.</param>
            /// <param name="page">The page number for pagination.</param>
            /// <param name="pageSize">The number of results per page.</param>
            /// <param name="order">The order in which to sort the results.</param>
            /// <param name="exactNumOfResults">Indicates whether to return an exact number of results.</param>
            /// <returns>A task that represents the asynchronous operation, containing the search result.</returns>
            public static Task<OsobaSearchResult> SimpleSearchAsync(string query, int page, int pageSize, string order,
                bool exactNumOfResults = false)
            {
                order = TextUtil.NormalizeToNumbersOnly(order);
                OrderResult eorder = OrderResult.Relevance;
                Enum.TryParse<OrderResult>(order, out eorder);

                return SimpleSearchAsync(query, page, pageSize, eorder, exactNumOfResults);
            }


            static string[] dontIndexOsoby = Config
                .GetWebConfigValue("DontIndexOsoby")
                .Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.ToLower())
                .ToArray();

            public static async Task<OsobaSearchResult> SimpleSearchAsync(string query, int page, int pageSize,
                OrderResult order, bool exactNumOfResults = false, int? osobaStatus = null)
            {
                //fix without elastic
                if (page < 1)
                    page = 1;
                var takeNum = page * pageSize;
                if (takeNum > 100)
                    takeNum = 100;
                //elastik hyr

                List<Osoba> foundPepole = new List<Osoba>();

                string regex = @"osoba\w{0,13}:\s?(?<osoba>[\w-]{3,50})";
                List<string> peopleIds = RegexUtil.GetRegexGroupValues(query, regex, "osoba").ToList();
                long total = peopleIds.LongCount();

                if (peopleIds is null || peopleIds.Count == 0)
                {
                    var people = await OsobyEsRepo.Searching.FulltextSearchAsync(query, page, pageSize, osobaStatus);
                    peopleIds = people.Results
                        .Where(r => r.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                        .Select(r => r.NameId).ToList();
                    total = total + people.Total;
                }

                foreach (var id in peopleIds)
                {
                    if (dontIndexOsoby.Contains(id) == false)
                    {
                        var foundPerson = OsobaRepo.GetByNameId(id);
                        if (foundPerson != null)
                        {
                            foundPepole.Add(foundPerson);
                        }
                        else
                            total = total - 1; // odecti neplatne osoby
                    }
                }

                var result = new OsobaSearchResult();
                result.Total = total;
                result.Q = query;
                result.ElasticResults = null; //TODO
                result.Results = foundPepole;
                result.Page = page;
                result.PageSize = pageSize;
                result.Order = order.ToString();
                result.IsValid = true;
                return result;
            }

            public static SortDescriptor<Osoba> GetSort(string sorder)
            {
                OrderResult order = OrderResult.Relevance;
                Enum.TryParse<OrderResult>(sorder, out order);
                return GetSort(order);
            }

            public static SortDescriptor<Osoba> GetSort(OrderResult order)
            {
                SortDescriptor<Osoba> s = new SortDescriptor<Osoba>().Field(f => f.Field("_score").Descending());
                switch (order)
                {
                    case OrderResult.FastestForScroll:
                        s = new SortDescriptor<Osoba>().Field(f => f.Field("_doc"));
                        break;
                    case OrderResult.NameAsc:
                        s = new SortDescriptor<Osoba>().Field(f => f.Field(ff => ff.Prijmeni).Ascending());
                        break;
                    case OrderResult.Relevance:
                    default:
                        break;
                }

                return s;
            }



            public record OsobaConfidence
            {
                public Osoba Osoba { get; set; }
                public decimal Confidence { get; set; }

                public FoundParts Found { get; set; } = FoundParts.None;

                [System.Flags]
                public enum FoundParts
                {
                    None = 0,
                    Status = 1 << 0,
                    DatumNarozeni = 1 << 1,
                    PolitickeFunkce = 1 << 2,
                    IcoMatch = 1 << 3,
                    NameWithAccents = 1 << 4,
                    SponzorPolitiku = 1 << 5,
                    All = ~0
                }
            }


            /// <summary>
            /// Represents a date that may be loose or imprecise, allowing for both specific dates and year ranges.
            /// </summary>
            public class LooseDate
            {
                public static LooseDate FromVek(int vek, DateTime forDate)
                {
                    throw new NotImplementedException();
                    return new LooseDate()
                    {

                    };
                }
                public LooseDate()
                { }
                /// <summary>
                /// Initializes a new instance of the <see cref="LooseDate"/> class.
                /// Attempts to parse the provided date string into a specific date or year.
                /// </summary>
                /// <param name="date">The date string to parse.</param>
                public LooseDate(string date)
                {
                    var dateString = FindDateInString(date);
                    if (dateString is not null)
                    {
                        this.Date = Devmasters.DT.Util.ToDate(dateString);
                    }

                    if (this.Date is null)
                    {
                        this.Year = GetYearFromString(date);
                    }
                }

                /// <summary>
                /// Gets or sets the specific date if parsed successfully.
                /// </summary>
                public DateTime? Date { get; set; }

                /// <summary>
                /// Gets or sets the year if a specific date could not be parsed.
                /// </summary>
                public int? Year { get; set; }

                private Devmasters.DT.DateInterval _di = null;

                /// <summary>
                /// Gets the year interval for the parsed year, if available.
                /// </summary>
                public Devmasters.DT.DateInterval YearInterval
                {
                    get
                    {
                        if (_di == null)
                        {
                            if (this.Year.HasValue)
                            {
                                _di = new Devmasters.DT.DateInterval(new DateTime(this.Year.Value, 1, 1), new DateTime(this.Year.Value, 12, 31));
                            }
                        }
                        return _di;
                    }
                }
            }
            private static OsobaConfidence OsobaConfidenceCalculation(Osoba o, DateTime? datumnarozeni,
                bool foundWithAccent,
                string[] isInIco = null)
            {
                isInIco = isInIco ?? Array.Empty<string>();
                decimal c = 0;
                var res = new OsobaConfidence() { Osoba = o, Confidence = 0 };
                if (foundWithAccent)
                {
                    c += 1;
                    res.Found |= OsobaConfidence.FoundParts.NameWithAccents;
                }

                //je to politik?
                var index = _politikStatusImportanceOrder.IndexOf(o.Status);
                if (index >= 0)
                {
                    c += 1 + ((_politikStatusImportanceOrder.Count - index) / 10m);
                    res.Found |= OsobaConfidence.FoundParts.Status;
                }
                if (datumnarozeni.HasValue && o.Narozeni.HasValue
                    && datumnarozeni == o.Narozeni)
                {
                    c = c + 10;
                    res.Found |= OsobaConfidence.FoundParts.DatumNarozeni;

                }
                //politicke funkce
                if (o.Events().Any(e => _politikImportanceEventTypes.Contains(e.Type)))
                {
                    c = c + (o.Events().Count(e => _politikImportanceEventTypes.Contains(e.Type)) / 10m);
                    res.Found |= OsobaConfidence.FoundParts.PolitickeFunkce;
                }

                if (isInIco != null && isInIco.Length > 0)
                {
                    var countIcos = o.Events()
                        .Where(m => !string.IsNullOrEmpty(m.Ico))
                        .Count(m => isInIco.Contains(m.Ico));
                    if (countIcos > 0)
                    {
                        c = c + 1 + (countIcos / 10);
                        res.Found |= OsobaConfidence.FoundParts.IcoMatch;
                    }
                }

                //sponzoruje politiky
                bool sponzoruje = OsobaRepo.PeopleWithAnySponzoringRecord(m => m.InternalId == o.InternalId)
                    .Any();
                if (sponzoruje)
                {
                    c = c + 1;
                    res.Found |= OsobaConfidence.FoundParts.SponzorPolitiku;
                }

                res.Confidence = c;
                return res;
            }
            public static OsobaConfidence[] FindOsobyWithConfidence(string jmeno_prijmeni, string datumNarozeni,
                string[] isInIco = null
                )
            {
                isInIco = isInIco ?? Array.Empty<string>();

                DateTime? fullDate = Devmasters.DT.Util.ToDate(FindDateInString(datumNarozeni));

                var nameParts = HlidacStatu.Entities.Validators.JmenaPrijmeniInText(jmeno_prijmeni);
                List<string> nameCombinations = HlidacStatu.Util.TextTools.GetPermutations(nameParts);
                List<OsobaConfidence> foundOsoby = new();
                foreach (var c in nameCombinations)
                {
                    var found = GetAllByName(c, datumNarozeni);
                    foundOsoby.AddRange(found.Select(m => OsobaConfidenceCalculation(m, fullDate, true, isInIco)));
                }
                if (foundOsoby.Count == 0)
                {
                    foreach (var c in nameCombinations)
                    {
                        var found = GetAllByNameAscii(c, datumNarozeni);
                        foundOsoby.AddRange(found.Select(m => OsobaConfidenceCalculation(m, fullDate, false, isInIco)));
                    }

                }

                var duplicates = foundOsoby.Where(oc => oc.Osoba.Status == (int)Osoba.StatusOsobyEnum.Duplicita);
                foreach (var duplicate in duplicates)
                {
                    if (duplicate.Osoba.OriginalId is not null)
                    {
                        var originalOsoba = OsobaRepo.GetByInternalId(duplicate.Osoba.OriginalId.Value);
                        if (originalOsoba != null)
                            duplicate.Osoba = originalOsoba;
                    }
                }



                return foundOsoby
                    .OrderByDescending(o => o.Confidence)
                    .DistinctBy(oc => oc.Osoba.NameId)
                    .ToArray();
            }

            public static Osoba GetByName(string jmeno, string prijmeni, DateTime narozeni)
            {
                var osoba = GetAllByName(jmeno, prijmeni, narozeni).FirstOrDefault();

                if (osoba is null || osoba.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                    return osoba;

                return osoba.GetOriginal();
            }



            /// Retrieves all Osoba entities matching the specified name and surname, with an optional date of birth.
            /// </summary>
            /// <param name="jmeno">The first name of the Osoba.</param>
            /// <param name="prijmeni">The surname of the Osoba.</param>
            /// <param name="narozeni">The date of birth of the Osoba, if available.</param>
            /// <returns>An enumerable collection of Osoba entities that match the specified criteria.</returns>
            public static Osoba[] GetAllByName(string jmeno, string prijmeni, DateTime? narozeni)
            {
                using (DbEntities db = new DbEntities())
                {
                    if (narozeni.HasValue)
                    {
                        return db.Osoba.AsNoTracking()
                              .Where(m =>
                                  m.Jmeno == jmeno
                                  && m.Prijmeni == prijmeni
                                  && m.Narozeni == narozeni
                              ).ToArray();
                    }
                    else
                    {
                        return db.Osoba.AsNoTracking()
                          .Where(m =>
                              m.Jmeno == jmeno
                              && m.Prijmeni == prijmeni
                          ).ToArray();
                    }
                }
            }



            public static IEnumerable<Osoba> GetAllByName(string jmeno, string prijmeni, string datumNarozeni)
            {
                LooseDate ld = new LooseDate(datumNarozeni);

                using (DbEntities db = new DbEntities())
                {
                    if (ld.Date.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                                && m.Narozeni == ld.Date
                            ).ToArray();
                    else if (ld.Year.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                                && m.Narozeni >= ld.YearInterval.From && m.Narozeni <= ld.YearInterval.To
                            ).ToArray();
                    return db.Osoba.AsNoTracking()
                        .Where(m =>
                            m.Jmeno == jmeno
                            && m.Prijmeni == prijmeni
                        ).ToArray();
                }
            }

            public static IEnumerable<Osoba> GetAllByNameAscii(string jmeno, string prijmeni, string datumNarozeni)
            {
                LooseDate ld = new LooseDate(datumNarozeni);


                using (DbEntities db = new DbEntities())
                {
                    if (ld.Date.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                                && m.Narozeni == ld.Date
                            ).ToArray();
                    else if (ld.Year.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                                && m.Narozeni >= ld.YearInterval.From && m.Narozeni <= ld.YearInterval.To
                            ).ToArray();
                    return db.Osoba.AsNoTracking()
                        .Where(m =>
                            m.JmenoAscii == jmeno
                            && m.PrijmeniAscii == prijmeni
                        ).ToArray();
                }
            }


            public static IEnumerable<Osoba> GetAllByName(string jmenoprijmeni, string datumNarozeni)
            {
                LooseDate ld = new LooseDate(datumNarozeni);

                using (DbEntities db = new DbEntities())
                {
                    if (ld.Date.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                (m.Jmeno + " " + m.Prijmeni) == jmenoprijmeni
                                && m.Narozeni == ld.Date.Value
                            ).ToArray();
                    else if (ld.Year.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                (m.Jmeno + " " + m.Prijmeni) == jmenoprijmeni
                                && m.Narozeni >= ld.YearInterval.From && m.Narozeni <= ld.YearInterval.To
                            ).ToArray();
                    return db.Osoba.AsNoTracking()
                        .Where(m =>
                                (m.Jmeno + " " + m.Prijmeni) == jmenoprijmeni
                        ).ToArray();
                }
            }

            public static IEnumerable<Osoba> GetAllByNameAscii(string jmenoprijmeni, string datumNarozeni)
            {
                jmenoprijmeni = jmenoprijmeni.NormalizeToPureTextLower().RemoveAccents();
                LooseDate ld = new LooseDate(datumNarozeni);


                using (DbEntities db = new DbEntities())
                {
                    if (ld.Date.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                (m.JmenoAscii + " " + m.PrijmeniAscii) == jmenoprijmeni
                                && m.Narozeni == ld.Date
                            ).ToArray();
                    else if (ld.Year.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                (m.JmenoAscii + " " + m.PrijmeniAscii) == jmenoprijmeni
                                && m.Narozeni >= ld.YearInterval.From && m.Narozeni <= ld.YearInterval.To
                            ).ToArray();
                    return db.Osoba.AsNoTracking()
                        .Where(m =>
                                (m.JmenoAscii + " " + m.PrijmeniAscii) == jmenoprijmeni
                        ).ToArray();
                }
            }


            // search all people by name, surname and dob
            public static IEnumerable<Osoba> FindAll(string name, string birthYear, bool extendedSearch = true, int take = 200)
            {
                if (string.IsNullOrWhiteSpace(name)
                    && string.IsNullOrWhiteSpace(birthYear))
                {
                    return Array.Empty<Osoba>();
                }

                string nquery = TextUtil.RemoveDiacritics(name.NormalizeToPureTextLower());
                birthYear = birthYear?.Trim();
                bool isValidYear = int.TryParse(birthYear, out int validYear);
                // diakritika, velikost

                if (extendedSearch)
                {
                    using (DbEntities db = new DbEntities())
                    {
                        System.FormattableString sql = $@"
                            select *
                              from Osoba os
                             where 
                               ((JmenoAscii + ' ' + PrijmeniAscii) LIKE {nquery} + '%'
                                    OR (PrijmeniAscii + ' ' + JmenoAscii) LIKE {nquery} + '%')
                               and (({validYear} = 0 OR YEAR(Narozeni) = {validYear}))";

                        return db.Osoba.FromSqlInterpolated(sql)
                            .AsNoTracking()
                            .Take(take)
                            .ToArray();
                    }
                }
                else
                {
                    return Politici.Get()
                        .Where(m =>
                            (
                                m.PrijmeniAscii.StartsWith(nquery, StringComparison.InvariantCultureIgnoreCase) == true
                                || m.JmenoAscii.StartsWith(nquery, StringComparison.InvariantCultureIgnoreCase) == true
                                || (m.JmenoAscii + " " + m.PrijmeniAscii).StartsWith(nquery,
                                    StringComparison.InvariantCultureIgnoreCase) == true
                                || (m.PrijmeniAscii + " " + m.JmenoAscii).StartsWith(nquery,
                                    StringComparison.InvariantCultureIgnoreCase) == true
                            )
                            && (!isValidYear || m.Narozeni?.Year == validYear)
                        )
                        .Take(take);
                }
            }

            public static IEnumerable<Osoba> GetPolitikByNameFtx(string jmeno, int maxNumOfResults = 1500)
            {
                string nquery = TextUtil.RemoveDiacritics(jmeno.NormalizeToPureTextLower());

                var res = PolitickyAktivni.Get()
                    .Where(m => m != null)
                    .Where(m =>
                        m.PrijmeniAscii?.StartsWith(nquery, StringComparison.InvariantCultureIgnoreCase) == true
                        || m.JmenoAscii?.StartsWith(nquery, StringComparison.InvariantCultureIgnoreCase) == true
                        || (m.JmenoAscii + " " + m.PrijmeniAscii)?.StartsWith(nquery,
                            StringComparison.InvariantCultureIgnoreCase) == true
                        || (m.PrijmeniAscii + " " + m.JmenoAscii)?.StartsWith(nquery,
                            StringComparison.InvariantCultureIgnoreCase) == true
                    )
                    .OrderByDescending(m => m.Status)
                    .ThenBy(m => m.Prijmeni)
                    .Take(maxNumOfResults);
                return res;
            }

            public static async Task<IEnumerable<Osoba>> GetPolitikByQueryFromFirmyAsync(string jmeno, int maxNumOfResults = 50,
                IEnumerable<Firma> alreadyFoundFirmyIcos = null)
            {
                var res = new Osoba[] { };

                var firmy = alreadyFoundFirmyIcos;
                if (firmy == null)
                    firmy = (await FirmaRepo.Searching.SimpleSearchAsync(jmeno, 0, maxNumOfResults * 10)).Result;

                if (firmy != null && firmy.Count() > 0)
                {
                    Dictionary<int, int> osoby = new Dictionary<int, int>();
                    bool skipRest = false;
                    foreach (var f in firmy)
                    {
                        if (skipRest)
                            break;

                        if (StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get().SoukromeFirmy.ContainsKey(f.ICO))
                        {
                            foreach (var osobaId in StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                                .SoukromeFirmy[f.ICO])
                            {
                                if (osoby.ContainsKey(osobaId))
                                    osoby[osobaId]++;
                                else
                                    osoby.Add(osobaId, 1);

                                if (osoby.Count > maxNumOfResults)
                                {
                                    skipRest = true;
                                    break;
                                }
                            }
                        }

                        if (skipRest == false)
                        {
                            var fvazby = f.AktualniVazby(Relation.AktualnostType.Nedavny);
                            foreach (var fv in fvazby)
                            {
                                if (fv.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                                {
                                    int osobaId = Convert.ToInt32(fv.To.Id);
                                    if (osoby.ContainsKey(osobaId))
                                        osoby[osobaId]++;
                                    else
                                        osoby.Add(osobaId, 1);
                                }

                                if (osoby.Count > maxNumOfResults)
                                {
                                    skipRest = true;
                                    break;
                                }

                                if (skipRest == false && StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                                    .SoukromeFirmy.ContainsKey(fv.To.Id))
                                {
                                    foreach (var osobaId in StaticData.FirmySVazbamiNaPolitiky_nedavne_Cache.Get()
                                        .SoukromeFirmy[fv.To.Id])
                                    {
                                        if (osoby.ContainsKey(osobaId))
                                            osoby[osobaId]++;
                                        else
                                            osoby.Add(osobaId, 1);
                                        if (osoby.Count > maxNumOfResults)
                                        {
                                            skipRest = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    res = osoby
                        .OrderByDescending(o => o.Value)
                        .Take(maxNumOfResults - res.Length)
                        .Select(m => Osoby.GetById.Get(m.Key))
                        .Where(m => m != null)
                        .Where(m => m.IsValid()) //not empty (nullObj from OsobaCache)
                        .ToArray();
                }

                return res;
            }


            public static List<int> PolitikStatusImportanceOrder = new List<int>() { 3, 4, 2, 1, 0 };
            public static int[] PolitikImportanceEventTypes = new int[]
            {
                (int) OsobaEvent.Types.Politicka, (int) OsobaEvent.Types.PolitickaExekutivni,
                (int) OsobaEvent.Types.VolenaFunkce
            };

            static List<int> _politikStatusImportanceOrder = new List<int>() {
                (int)StatusOsobyEnum.Politik,
                (int)StatusOsobyEnum.ByvalyPolitik,
                (int)StatusOsobyEnum.VazbyNaPolitiky,
                (int)StatusOsobyEnum.Sponzor,
                (int)StatusOsobyEnum.VysokyUrednik,
            };

            static int[] _politikImportanceEventTypes = new int[]
            {
                (int) OsobaEvent.Types.Politicka, (int) OsobaEvent.Types.PolitickaExekutivni,
                (int) OsobaEvent.Types.VolenaFunkce
            };



            public static async Task<IEnumerable<Osoba>> GetAllPoliticiFromTextAsync(string text)
            {
                var parsedName = await Repositories.Searching.Politici.FindCitationsAsync(text); //Validators.JmenoInText(text);

                var oo = parsedName.Select(nm => Osoby.GetByNameId.Get(nm))
                    .Where(o => o != null)
                    .OrderPoliticiByImportance();
                return oo;
            }

            public static async Task<IEnumerable<Osoba>> GetBestPoliticiFromTextAsync(string text)
            {
                List<Osoba> uniqO = new List<Osoba>();
                var oo = await GetAllPoliticiFromTextAsync(text);
                foreach (var o in oo)
                {
                    if (
                        !uniqO.Any(m => (m.NameId != o.NameId && m.Jmeno == o.Jmeno && m.Prijmeni == o.Prijmeni))
                    )
                        uniqO.Add(o);
                }

                var ret = uniqO.OrderPoliticiByImportance();

                return ret;
            }

            public static async Task<Osoba> GetFirstPolitikFromTextAsync(string text)
            {
                var osoby = await GetBestPoliticiFromTextAsync(text);
                if (osoby.Count() == 0)
                    return null;

                return osoby.First();
            }

            public static Osoba GetByNameAscii(string jmeno, string prijmeni, DateTime narozeni)
            {
                return GetAllByNameAscii(jmeno, prijmeni, narozeni).FirstOrDefault();
            }

            public static IEnumerable<Osoba> GetAllByNameAscii(string jmeno, string prijmeni, DateTime? narozeni)
            {
                jmeno = TextUtil.RemoveDiacritics(jmeno);
                prijmeni = TextUtil.RemoveDiacritics(prijmeni);
                using (DbEntities db = new DbEntities())
                {
                    if (narozeni.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                                && (m.Narozeni == narozeni)
                            ).ToArray();
                    else
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                            ).ToArray();
                }
            }

            private static int? GetYearFromString(string? yearstring)
            {
                if (yearstring is null)
                    return null;

                var match = YearRegexLoose.Match(yearstring);
                if (match.Success)
                {
                    var year = int.Parse(match.Value);
                    return year;
                }

                return null;
            }

            private static string? FindDateInString(string? dateString)
            {
                if (dateString is null)
                    return null;

                var match = DateRegex.Match(dateString);
                if (match.Success)
                {
                    return match.Value;
                }

                return null;
            }
        }




    }
}