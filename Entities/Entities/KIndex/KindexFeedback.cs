using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    [ElasticsearchType(IdProperty = nameof(Id))]
    public class KindexFeedback
    {
        [Keyword]
        public string Id { get; set; }
        [Keyword]
        public int Year { get; set; }
        [Date]
        public DateTime? SignDate { get; set; }
        [Keyword]
        public string Ico { get; set; }
        [Text]
        public string Company { get; set; }
        [Text]
        public string Text { get; set; }
        [Text]
        public string Author { get; set; }

        public async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                Id = Guid.NewGuid().ToString();
            }
            if (string.IsNullOrWhiteSpace(Company))
            {
                var firma = FirmaRepo.FromIco(Ico);
                Company = firma.Jmeno;
            }

            var client = await Manager.GetESClient_KindexFeedbackAsync();
            var res = await client.IndexDocumentAsync(this); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

        public static async Task<IEnumerable<KindexFeedback>> GetKindexFeedbacksAsync(string ico, int year)
        {
            ElasticClient esClient = await Manager.GetESClient_KindexFeedbackAsync();

            ISearchResponse<KindexFeedback> searchResults = null;
            try
            {
                searchResults = await esClient.SearchAsync<KindexFeedback>(s =>
                        s.Query(q =>
                            q.Term(f => f.Ico, ico)
                            && q.Term(f => f.Year, year)
                            )
                        );

                if (searchResults.IsValid && searchResults.Hits.Count > 0)
                {
                    var hits = searchResults.Hits.Select(h => h.Source).OrderByDescending(s => s.SignDate);
                    return hits;
                }
            }
            catch (Exception e)
            {
                string origQuery = $"ico:{ico}; year:{year};";
                AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
                if (searchResults != null && searchResults.ServerError != null)
                {
                    Manager.LogQueryError<KindexFeedback>(searchResults,
                        $"Exception for {origQuery}",
                        ex: e);
                }
                else
                {
                    Util.Consts.Logger.Error(origQuery, e);
                }
            }

            return Enumerable.Empty<KindexFeedback>();
        }

        public static async Task<KindexFeedback> GetByIdAsync(string id)
        {

            ElasticClient esClient = await Manager.GetESClient_KindexFeedbackAsync();

            ISearchResponse<KindexFeedback> searchResults = null;
            try
            {
                searchResults = await esClient.SearchAsync<KindexFeedback>(s =>
                        s.Query(q =>
                            q.Term(f => f.Id, id)
                            )
                        );

                if (searchResults.IsValid && searchResults.Hits.Count > 0)
                {
                    var hits = searchResults.Hits.Select(h => h.Source).OrderByDescending(s => s.SignDate);
                    return hits.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                string origQuery = $"id:{id};";
                AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
                if (searchResults != null && searchResults.ServerError != null)
                {
                    Manager.LogQueryError<KindexFeedback>(searchResults,
                        $"Exception for {origQuery}",
                        ex: e);
                }
                else
                {
                    Util.Consts.Logger.Error(origQuery, e);
                }
            }

            return null;
        }
    }
}
