using Devmasters;

using HlidacStatu.Entities;
using HlidacStatu.Searching;
using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using NuGet.Common;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public static class Searching
        {
            private static readonly AsyncLazy<Dictionary<int, string[]>> AllValuesLazy = new(GetAllValuesAsync);
            public static async Task<Dictionary<int, string[]>> AllValuesAsync() => await AllValuesLazy;

            private static async Task<Dictionary<int, string[]>> GetAllValuesAsync()
            {
                var client = Manager.GetESClient_RPP_Kategorie();
                var res = await client.SearchAsync<Lib.Data.External.RPP.KategorieOVM>(
                    s => s.Query(q => q.MatchAll()).Size(2000)
                    );
                return res.Hits
                    .Select(m => new { id = m.Source.id, icos = m.Source.OVM_v_kategorii.Select(o => o.kodOvm).ToArray() })
                    .ToDictionary(k => k.id, v => v.icos);
            }

            public static async Task<Search.GeneralResult<Firma>> SimpleSearchAsync(string query, int page, int size, bool keepOnlyActive = false)
            {
                List<Firma> found = new List<Firma>();

                string modifQ = SimpleQueryCreator
                    .GetSimpleQuery(query,
                        new IRule[] { new Firmy_OVMKategorie(await AllValuesAsync()) })
                    .FullQuery();

                string[] specifiedIcosInQuery =
                    RegexUtil.GetRegexGroupValues(modifQ, @"(ico\w{0,11}\: \s? (?<ic>\d{3,8}))", "ic");
                if (specifiedIcosInQuery != null && specifiedIcosInQuery.Length > 0)
                {
                    foreach (var ic in specifiedIcosInQuery.Skip((page - 1) * size).Take(size))
                    {
                        Firma f = Firmy.Get(ic);
                        if (f?.Valid == true && FirmaRepo.IgnoredIcos.Contains(f.ICO) == false)
                        {
                            //nalezene ICO
                            found.Add(f);
                        }
                    }

                    if (found.Count() > 0)
                        return new Search.GeneralResult<Firma>(query, specifiedIcosInQuery.Count(), found,
                                size, true)
                        { Page = page };
                }

                page = page - 1;
                if (page < 0)
                    page = 0;

                if (page * size >= Manager.MaxResultWindow) //elastic limit
                {
                    page = 0;
                    size = 0; //return nothing
                }


                var qc = GetSimpleQuery(query);


                ISearchResponse<FirmaInElastic> res = null;
                try
                {
                    var client = Manager.GetESClient_Firmy(); 
                    res = await client.SearchAsync<FirmaInElastic>(s => s
                            .Size(size)
                            .From(page * size)
                            .TrackTotalHits(size == 0 ? true : (bool?)null)
                            .Query(q => qc)
                        );

                    if (res.IsValid)
                    {
                        foreach (var i in res.Hits)
                        {
                            if (FirmaRepo.IgnoredIcos.Contains(i.Source.Ico) == false)
                                found.Add(Firmy.Get(i.Source.Ico));
                        }

                        if (keepOnlyActive)
                            found = found.Where(m => m.Status < 6).ToList(); //see Firma.StatusFull()

                        return new Search.GeneralResult<Firma>(query, res.Total, found, size, true)
                        { Page = page };
                    }
                    else
                    {
                        Manager.LogQueryError<FirmaInElastic>(res, query);
                        return new Search.GeneralResult<Firma>(query, 0, found, size, false) { Page = page };
                    }
                }
                catch (Exception e)
                {
                    if (res != null && res.ServerError != null)
                        Manager.LogQueryError<FirmaInElastic>(res, query);
                    else
                        _logger.Error(e, "");
                    throw;
                }
            }

            public static QueryContainer GetSimpleQuery(string query)
            {
                var qc = SimpleQueryCreator.GetSimpleQuery<FirmaInElastic>(query,
                    new IRule[] { });
                return qc;
            }


            public static async Task<IEnumerable<Firma>> FindAllAsync(string query, int limit, bool keepOnlyActive = false)
            {
                return (await SimpleSearchAsync(query, 0, limit,keepOnlyActive)).Result;
            }

        }

    }
}