using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities.Dotace;
using HlidacStatu.Repositories.ES;
using Nest;
using HlidacStatu.Repositories.Searching;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        
        public static Dotace Get(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = Manager.GetESClient_Dotace().Get<Dotace>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }
        
        public static IEnumerable<Dotace> GetAll(string scrollTimeout = "2m", int scrollSize = 300)
        {
            return YieldAll(null, scrollTimeout, scrollSize);

        }
        
        public static void Save(Dotace dotace)
        {
            if (dotace == null) throw new ArgumentNullException(nameof(dotace));

            dotace.CalculateTotals();
            dotace.CalculateCerpaniYears();

            var client = Manager.GetESClient_Dotace();
            var res = client.Index(dotace, o => o.Id(dotace.IdDotace)); //druhy parametr musi byt pole, ktere je unikatni
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
        public static bool BulkSave(List<Dotace> dotace)
        {
            foreach (var d in dotace)
            {
                d.CalculateTotals();
                d.CalculateCerpaniYears();
            }

            var result = Manager.GetESClient_Dotace().IndexMany(dotace);

            if (result.Errors)
            {
                var a = result.DebugInformation;
                Util.Consts.Logger.Error($"Error when bulkSaving dotace to ES: {a}");
            }

            return result.Errors;
        }
        
        public static (decimal Sum, int Count) GetStatisticsForIco(string ico)
        {
            var dotaceAggs = new AggregationContainerDescriptor<Dotace>()
                .Sum("souhrn", s => s
                    .Field(f => f.DotaceCelkem)
                );

            var dotaceSearch = Searching.SimpleSearch($"ico:{ico}", 1, 1,
                DotaceSearchResult.DotaceOrderResult.FastestForScroll, false,
                dotaceAggs, exactNumOfResults: true);

            decimal sum = (decimal)dotaceSearch.ElasticResults.Aggregations.Sum("souhrn").Value;
            int count = (int)dotaceSearch.Total;

            return (sum, count);
        }

        public static Dictionary<string, (decimal Sum, int Count)> GetStatisticsForHolding(string ico)
        {
            var dotaceAggsH = new AggregationContainerDescriptor<Dotace>()
                .Terms("icos", s => s
                    .Field(f => f.Prijemce.Ico)
                    .Size(5000)
                    .Aggregations(a => a
                        .Sum("sum", ss => ss.Field(ff => ff.DotaceCelkem))
                    )
                );
            var dotaceSearchH = Searching.SimpleSearch($"holding:{ico}", 1, 1,
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

        public static IEnumerable<Dotace> GetDotaceForIco(string ico)
        {
            QueryContainer qc = new QueryContainerDescriptor<Dotace>()
                .Term(f => f.Prijemce.Ico, ico);

            return YieldAll(qc);
        }

        public static IEnumerable<Dotace> GetDotaceForHolding(string holdingIco)
        {
            string query = Tools.FixInvalidQuery($"holding:{holdingIco}", Searching.QueryShorcuts(), Searching.QueryOperators);
            var qc = SimpleQueryCreator.GetSimpleQuery<Dotace>(query, Searching.Irules);

            //QueryContainer qc = new QueryContainerDescriptor<Dotace>()
            //    .Term(f => f.Prijemce.Ico, holdingIco);

            return YieldAll(qc);
        }
        
        
        private static IEnumerable<Dotace> YieldAll(QueryContainer query, 
            string scrollTimeout = "2m", 
            int scrollSize = 300)
        {
            ISearchResponse<Dotace> initialResponse = null;
            if (query is null)
            {
                initialResponse = Manager.GetESClient_Dotace().Search<Dotace>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .MatchAll()
                    .Scroll(scrollTimeout));
            }
            else
            {
                initialResponse = Manager.GetESClient_Dotace().Search<Dotace>(scr => scr
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
                ISearchResponse<Dotace> loopingResponse = Manager.GetESClient_Dotace().Scroll<Dotace>(scrollTimeout, scrollid);
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

            Manager.GetESClient_Dotace().ClearScroll(new ClearScrollRequest(scrollid));
        }
        
    }
}