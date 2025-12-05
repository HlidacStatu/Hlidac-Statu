using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.XLib.Render;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Datasets;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Web.Framework
{
    public static class LocalHtmlExtensions
    {

        public static IHtmlContent SubjektTypTrojice(this IHtmlHelper self, Firma firma, string htmlProOVM, string htmlProStatniFirmu, string htmlProSoukromou)
        {
            if (firma == null)
                return self.Raw("");
            if (firma.JsemOVM())
                return self.Raw(htmlProOVM);
            else if (firma.JsemStatniFirma())
                return self.Raw(htmlProStatniFirmu);
            else
                return self.Raw(htmlProSoukromou);
        }


        public static IHtmlContent RenderVazby(this IHtmlHelper self, HlidacStatu.DS.Graphs.Graph.Edge[] vazbyToRender)
        {
            if (vazbyToRender == null)
            {
                return self.Raw("");
            }
            if (vazbyToRender.Count() == 0)
            {
                return self.Raw("");
            }

            if (vazbyToRender.Count() == 1)
                return self.Raw($"{vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}");
            else
            {
                return self.Raw("Nepřímá vazba přes:<br/><small>"
                    + $"{vazbyToRender.First().From?.PrintName()} {vazbyToRender.First().Descr} v {vazbyToRender.First().To.PrintName()} {vazbyToRender.First().Doba()}"
                    + $" → "
                    + vazbyToRender.Skip(1).Select(m => m.Descr + " v " + m.To.PrintName()).Aggregate((f, s) => f + " → " + s)
                    + "</small>"
                    );
            }
        }
        
        public static async Task<IHtmlContent> RenderDatasetResultsAsync(this IHtmlHelper self, IDatasetResult dsResult)
        {
            var items = await Task.WhenAll(
                dsResult.Results
                    .Where(m => m.Total > 0)
                    .OrderByDescending(o => o.Total)
                    .Take(4)
                    .Select(async m =>
                    {
                        var reg = await m.DataSet.RegistrationAsync();
                        return $"<li><a href='{m.DataSet.DatasetSearchUrl(m.RenderQuery())}'>{reg.name}</a> ({HlidacStatu.Util.RenderData.NiceNumber(m.Total)})</li>";
                    })
            );

            var html = string.Join("", items);
            return self.Raw(html);
        }

        
        public static async Task<(IEnumerable<string> Tabs, IEnumerable<string> Results)> NalezeneVysledkyVDalsichDatabazichAsync(ConcurrentBag<DataSearchResult> datasetResults, string query)
        {
            List<string> tabs = new();
            foreach (var rds in datasetResults.Where(m => m.Total > 0).OrderByDescending(m => m.Total))
            {
                tabs.Add($"{(await rds.DataSet.RegistrationAsync()).name}&nbsp;({HlidacStatu.Util.RenderData.Vysledky.PocetVysledku(rds.Total)})");
            }
            
            List<string> results = new();
            foreach (var rds in datasetResults
                         .Where(m => m.Total > 0)
                         .OrderByDescending(m => m.Total))
            {
                results.Add(await rds.RenderResultsInHtmlAsync(query));
            }
            
            return (tabs, results);
        }

        public static async Task<(int Id, string Name, long Count)[]> GetVzList()
        {
            var cpv = Devmasters.Enums.EnumTools.EnumToEnumerable(
                typeof(VerejnaZakazkaRepo.Searching.CPVSkupiny));

            using var sem = new SemaphoreSlim(10);
            Task<(int Id, string Name, long Count)>[] tasks = cpv.Select(async m =>
            {
                await sem.WaitAsync();
                try
                {
                    var result = await VzCache.CachedSimpleSearchAsync(
                        new HlidacStatu.Repositories.Searching.VerejnaZakazkaSearchData
                        {
                            Q = "zverejneno:[" + DateTime.Now.Date.AddMonths(-12).ToString("yyyy-MM-dd") + " TO *]",
                            Oblast = m.Id.ToString(),
                            Page = 0,
                            PageSize = 0,
                            ExactNumOfResults = true
                        });

                    return (m.Id, m.Name, result.Total);
                    
                }
                finally
                {
                    sem.Release();
                }
            }).ToArray();

            return await Task.WhenAll(tasks);
        }

    }
}