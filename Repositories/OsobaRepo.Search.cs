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

            public static Osoba GetByName(string jmeno, string prijmeni, DateTime narozeni)
            {
                var osoba = GetAllByName(jmeno, prijmeni, narozeni).FirstOrDefault();

                if (osoba is null || osoba.Status != (int)Osoba.StatusOsobyEnum.Duplicita)
                    return osoba;

                return osoba.GetOriginal();
            }

            public static IEnumerable<Osoba> GetAllByName(string jmeno, string prijmeni, DateTime? narozeni)
            {
                using (DbEntities db = new DbEntities())
                {
                    if (narozeni.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                                && m.Narozeni == narozeni
                            ).ToArray();
                    else
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                            ).ToArray();
                }
            }
            
            public static IEnumerable<Osoba> GetAllByName(string jmeno, string prijmeni, string datumNarozeni)
            {
                DateTime? fullDate = null;
                DateTime? startDate = null;
                DateTime? endDate = null;
                if (!string.IsNullOrWhiteSpace(datumNarozeni))
                {
                    var dateString = FindDateInString(datumNarozeni);
                    if (dateString is not null)
                    {
                        fullDate = Devmasters.DT.Util.ParseDateTime(dateString, null);
                    }
            
                    if (fullDate is null)
                    {
                        var birthYear = GetYearFromString(datumNarozeni);
                        if (birthYear is not null)
                        {
                            startDate = new DateTime(birthYear.Value, 1, 1);
                            endDate = new DateTime(birthYear.Value, 12, 31);
                            
                        }
                    }
                }
                
                using (DbEntities db = new DbEntities())
                {
                    if (fullDate.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                                && m.Narozeni == fullDate
                            ).ToArray();
                    else if(startDate.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.Jmeno == jmeno
                                && m.Prijmeni == prijmeni
                                && m.Narozeni >= startDate && m.Narozeni <= endDate
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
                jmeno = jmeno.NormalizeToPureTextLower().RemoveAccents();
                prijmeni = prijmeni.NormalizeToPureTextLower().RemoveAccents();
                DateTime? fullDate = null;
                DateTime? startDate = null;
                DateTime? endDate = null;
                if (!string.IsNullOrWhiteSpace(datumNarozeni))
                {
                    var dateString = FindDateInString(datumNarozeni);
                    if (dateString is not null)
                    {
                        fullDate = Devmasters.DT.Util.ParseDateTime(dateString, null);
                    }
            
                    if (fullDate is null)
                    {
                        var birthYear = GetYearFromString(datumNarozeni);
                        if (birthYear is not null)
                        {
                            startDate = new DateTime(birthYear.Value, 1, 1);
                            endDate = new DateTime(birthYear.Value, 12, 31);
                            
                        }
                    }
                }
                
                using (DbEntities db = new DbEntities())
                {
                    if (fullDate.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                                && m.Narozeni == fullDate
                            ).ToArray();
                    else if(startDate.HasValue)
                        return db.Osoba.AsNoTracking()
                            .Where(m =>
                                m.JmenoAscii == jmeno
                                && m.PrijmeniAscii == prijmeni
                                && m.Narozeni >= startDate && m.Narozeni <= endDate
                            ).ToArray();
                    return db.Osoba.AsNoTracking()
                        .Where(m =>
                            m.JmenoAscii == jmeno
                            && m.PrijmeniAscii == prijmeni
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

            public static List<int> PolitikImportanceOrder = new List<int>() { 3, 4, 2, 1, 0 };

            public static int[] PolitikImportanceEventTypes = new int[]
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