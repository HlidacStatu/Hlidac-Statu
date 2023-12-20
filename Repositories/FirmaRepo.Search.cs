using Devmasters;

using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Searching.Rules;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public static class Searching
        {
            public const int MaxResultWindow = 10000;

            static string[] ignoredIcos = Config
                .GetWebConfigValue("DontIndexFirmy")
                .Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.ToLower())
                .ToArray();

            public static async Task<Search.GeneralResult<Firma>> SimpleSearchAsync(string query, int page, int size)
            {
                List<Firma> found = new List<Firma>();

                string modifQ = SimpleQueryCreator
                    .GetSimpleQuery(query,
                        new IRule[] { new Firmy_OVMKategorie() })
                    .FullQuery();

                string[] specifiedIcosInQuery =
                    RegexUtil.GetRegexGroupValues(modifQ, @"(ico\w{0,11}\: \s? (?<ic>\d{3,8}))", "ic");
                if (specifiedIcosInQuery != null && specifiedIcosInQuery.Length > 0)
                {
                    foreach (var ic in specifiedIcosInQuery.Skip((page - 1) * size).Take(size))
                    {
                        Firma f = Firmy.Get(ic);
                        if (f.Valid && ignoredIcos.Contains(f.ICO) == false)
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

                if (page * size >= MaxResultWindow) //elastic limit
                {
                    page = 0;
                    size = 0; //return nothing
                }


                var qc = GetSimpleQuery(query);


                ISearchResponse<FirmaInElastic> res = null;
                try
                {
                    var client = await Manager.GetESClient_FirmyAsync(); 
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
                            if (ignoredIcos.Contains(i.Source.Ico) == false)
                                found.Add(Firmy.Get(i.Source.Ico));
                        }

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


            public static async Task<IEnumerable<Firma>> FindAllAsync(string query, int limit)
            {
                return (await SimpleSearchAsync(query, 0, limit)).Result;
            }

        }

    }
}