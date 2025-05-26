using HlidacStatu.Entities;
using HlidacStatu.Entities.OsobyES;
using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class OsobyEsRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(OsobyEsRepo));

        private static readonly ElasticClient _esClient = Manager.GetESClient_Osoby();

        public static async Task<bool> DeleteAllAsync()
        {
            var response = await _esClient.DeleteByQueryAsync<OsobaES>(m => m.MatchAll());
            return response.IsValid;
        }

        public static async Task BulkSaveAsync(IEnumerable<OsobaES> osoby)
        {
            var result = await _esClient.IndexManyAsync<OsobaES>(osoby);

            if (result.Errors)
            {
                var a = result.DebugInformation;
                _logger.Error($"Error when bulkSaving osoby to ES: {a}");
            }
        }

        public static async IAsyncEnumerable<OsobaES> YieldAllPoliticiansAsync(string scrollTimeout = "2m", int scrollSize = 1000)
        {
            ISearchResponse<OsobaES> initialResponse = await _esClient.SearchAsync<OsobaES>
            (scr => scr.From(0)
                .Take(scrollSize)
                .Query(_query => _query.Term(_field => _field.Status, (int)Osoba.StatusOsobyEnum.Politik))
                .Scroll(scrollTimeout));

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                foreach (var osoba in initialResponse.Documents)
                {
                    yield return osoba;
                }

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<OsobaES> loopingResponse = await _esClient.ScrollAsync<OsobaES>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    foreach (var osoba in loopingResponse.Documents)
                    {
                        yield return osoba;
                    }

                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            await _esClient.ClearScrollAsync(new ClearScrollRequest(scrollid));
        }
    }
}