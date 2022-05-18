using HlidacStatu.Entities.Dotace;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.Searching;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        
        private static ElasticClient _dotaceClient = await Manager.GetESClient_DotaceAsync();

        public static async Task<Dotace> GetAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await _dotaceClient.GetAsync<Dotace>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }

        public static IAsyncEnumerable<Dotace> GetAllAsync(string scrollTimeout = "2m", int scrollSize = 300)
        {
            return YieldAllAsync(null, scrollTimeout, scrollSize);

        }

        public static async Task SaveAsync(Dotace dotace)
        {
            if (dotace == null) throw new ArgumentNullException(nameof(dotace));

            dotace.CalculateTotals();
            dotace.CalculateCerpaniYears();

            var res = await _dotaceClient.IndexAsync(dotace, o => o.Id(dotace.IdDotace)); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }


        /// <summary>
        /// Returns true if any error ocurred.
        /// </summary>
        /// <param name="dotace"></param>
        /// <returns>True if any error occured during save.</returns>
        public static async Task<bool> BulkSaveAsync(List<Dotace> dotace)
        {
            foreach (var d in dotace)
            {
                d.CalculateTotals();
                d.CalculateCerpaniYears();
            }

            var result = await _dotaceClient.IndexManyAsync(dotace);

            if (result.Errors)
            {
                var a = result.DebugInformation;
                Util.Consts.Logger.Error($"Error when bulkSaving dotace to ES: {a}");
            }

            return result.Errors;
        }

        public static async Task<(decimal Sum, int Count)> GetStatisticsForIcoAsync(string ico)
        {
            var dotaceAggs = new AggregationContainerDescriptor<Dotace>()
                .Sum("souhrn", s => s
                    .Field(f => f.DotaceCelkem)
                );

            var dotaceSearch = await Searching.SimpleSearchAsync($"ico:{ico}", 1, 1,
                DotaceSearchResult.DotaceOrderResult.FastestForScroll, false,
                dotaceAggs, exactNumOfResults: true);

            decimal sum = (decimal)dotaceSearch.ElasticResults.Aggregations.Sum("souhrn").Value;
            int count = (int)dotaceSearch.Total;

            return (sum, count);
        }

        public static async Task<Dictionary<string, (decimal Sum, int Count)>> GetStatisticsForHoldingAsync(string ico)
        {
            var dotaceAggsH = new AggregationContainerDescriptor<Dotace>()
                .Terms("icos", s => s
                    .Field(f => f.Prijemce.Ico)
                    .Size(5000)
                    .Aggregations(a => a
                        .Sum("sum", ss => ss.Field(ff => ff.DotaceCelkem))
                    )
                );
            var dotaceSearchH = await Searching.SimpleSearchAsync($"holding:{ico}", 1, 1,
                DotaceSearchResult.DotaceOrderResult.FastestForScroll, false,
                dotaceAggsH, exactNumOfResults: true);

            var items = ((BucketAggregate)dotaceSearchH.ElasticResults.Aggregations["icos"]).Items;

            Dictionary<string, (decimal Sum, int Count)> dict = items.ToDictionary(
                i => ((KeyedBucket<object>)i).Key.ToString(),
                i => ((decimal)((KeyedBucket<object>)i).Sum("sum").Value,
                    (int)((KeyedBucket<object>)i).DocCount)
                );

            return dict;
        }

        public static IAsyncEnumerable<Dotace> GetDotaceForIcoAsync(string ico)
        {
            QueryContainer qc = new QueryContainerDescriptor<Dotace>()
                .Term(f => f.Prijemce.Ico, ico);

            return YieldAllAsync(qc);
        }

        public static IAsyncEnumerable<Dotace> GetDotaceForHoldingAsync(string holdingIco)
        {
            string query = Tools.FixInvalidQuery($"holding:{holdingIco}", Searching.QueryShorcuts(), Searching.QueryOperators);
            var qc = SimpleQueryCreator.GetSimpleQuery<Dotace>(query, Searching.Irules);

            return YieldAllAsync(qc);
        }


        private static async IAsyncEnumerable<Dotace> YieldAllAsync(QueryContainer query,
            string scrollTimeout = "2m",
            int scrollSize = 300)
        {
            ISearchResponse<Dotace> initialResponse = null;
            if (query is null)
            {
                initialResponse = await _dotaceClient.SearchAsync<Dotace>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .MatchAll()
                    .Scroll(scrollTimeout));
            }
            else
            {
                initialResponse = await _dotaceClient.SearchAsync<Dotace>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .Query(q => query)
                    .Scroll(scrollTimeout));
            }

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                foreach (var dotace in initialResponse.Documents)
                {
                    yield return dotace;
                }

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<Dotace> loopingResponse = await _dotaceClient.ScrollAsync<Dotace>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    foreach (var dotace in loopingResponse.Documents)
                    {
                        yield return dotace;
                    }
                    scrollid = loopingResponse.ScrollId;
                }
                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            await _dotaceClient.ClearScrollAsync(new ClearScrollRequest(scrollid));
        }

    }
}