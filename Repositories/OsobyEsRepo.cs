using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OsobyES;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories
{
    public static partial class OsobyEsRepo
    {
        private static readonly ElasticClient _esClient = Manager.GetESClient_Osoby();

        public static bool DeleteAll()
        {
            var response = _esClient.DeleteByQuery<OsobaES>(m => m.MatchAll());
            return response.IsValid;
        }

        public static OsobaES Get(string idOsoby)
        {
            var response = _esClient.Get<OsobaES>(idOsoby);

            return response.IsValid
                ? response.Source
                : null;
        }

        public static void BulkSave(IEnumerable<OsobaES> osoby)
        {
            var result = _esClient.IndexMany<OsobaES>(osoby);

            if (result.Errors)
            {
                var a = result.DebugInformation;
                Util.Consts.Logger.Error($"Error when bulkSaving osoby to ES: {a}");
            }
        }

        public static IEnumerable<OsobaES> YieldAllPoliticians(string scrollTimeout = "2m", int scrollSize = 1000)
        {
            ISearchResponse<OsobaES> initialResponse = _esClient.Search<OsobaES>
            (scr => scr.From(0)
                .Take(scrollSize)
                .Query(_query => _query.Term(_field => _field.Status, (int) Osoba.StatusOsobyEnum.Politik))
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
                ISearchResponse<OsobaES> loopingResponse = _esClient.Scroll<OsobaES>(scrollTimeout, scrollid);
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

            _esClient.ClearScroll(new ClearScrollRequest(scrollid));
        }
    }
}