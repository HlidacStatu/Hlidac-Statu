using HlidacStatu.Connectors;
using HlidacStatu.Entities.Insolvence;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;

namespace HlidacStatu.Repositories
{
    public static class SearchableDocumentRepo
    {

        public static async Task<Entities.Insolvence.SearchableDocument[]> GetAllAsync(string spisovaZnacka, bool includePrilohy = false)
        {

            var client = await Manager.GetESClient_InsolvenceDocsAsync();

            var res = includePrilohy ?
                await client.SearchAsync<SearchableDocument>(s => s
                    .Query(q => q
                        .QueryString(qs => qs.Query($"spisovaZnacka:\"{spisovaZnacka}\"")
                            )
                        )
                        .Size(9999)
                    ) :
                    await client.SearchAsync<SearchableDocument>(s => s
                        .Query(q => q
                            .QueryString(qs => qs.Query($"spisovaZnacka:\"{spisovaZnacka}\"")
                                )
                            )
                            .Size(9999)
                            .Source(s=>s.Excludes(e=>e.Field(f=>f.PlainText)))
                        );
            if (res.IsValid)
                return res.Hits.Select(m => m.Source).ToArray();
            else
                return null;
        }
        public static async Task<Entities.Insolvence.SearchableDocument> GetAsync(string spisovaZnacka, string id, bool includePrilohy = false)
        {
            spisovaZnacka = Rizeni.NormalizedId(spisovaZnacka);
            var searchableDocId = SearchableDocument.GetDocumentId(spisovaZnacka, id);
            return await GetAsync(searchableDocId, includePrilohy);
        }
        public static async Task<Entities.Insolvence.SearchableDocument> GetAsync(string searchableDocId, bool includePrilohy = false)
        {
            var client = await Manager.GetESClient_InsolvenceDocsAsync();
            var res = includePrilohy
                ? await client.GetAsync<SearchableDocument>(searchableDocId)
                : await client.GetAsync<SearchableDocument>(searchableDocId, s => s.SourceExcludes(s=>s.PlainText));

            if (res.IsValid && res.Found)
                return res.Source;
            else
                return null;
        }

        public static async Task<bool> Exists(string spisovaZnacka, string dokumentId)
        {

            var client = await Manager.GetESClient_InsolvenceDocsAsync();

            spisovaZnacka = Rizeni.NormalizedId(spisovaZnacka);
            var docid = SearchableDocument.GetDocumentId(spisovaZnacka, dokumentId);

            var res = await client.DocumentExistsAsync<SearchableDocument>(docid);
            if (res.IsValid)
                return res.Exists;
            else
                return false;
        }
        public static async Task<long> HowMany(string spisovaZnacka)
        {

            var client = await Manager.GetESClient_InsolvenceDocsAsync();

            var res = await client.SearchAsync<SearchableDocument>(s => s
                    .Query(q => q
                        .QueryString(qs => qs.Query($"spisovaZnacka:\"{spisovaZnacka}\"")
                    )

                )
                .Size(0)
                .TrackTotalHits(true)
            );
            if (res.IsValid)
                return res.Total;
            else
                return 0;
        }
        public static async Task<string[]> AllIds(string spisovaZnacka)
        {

            var client = await Manager.GetESClient_InsolvenceDocsAsync();

            var res = await client.SearchAsync<SearchableDocument>(s => s
                        .Query(q => q
                            .QueryString(qs => qs.Query($"spisovaZnacka:\"{spisovaZnacka}\"")
                                )
                            )
                            .Size(9999)
                            .Source(false)
                        );
            if (res.IsValid)
                return res.Hits.Select(m => m.Id).ToArray();
            else
                return null;
        }


        public static async Task SaveManyAsync(IEnumerable<SearchableDocument> sDocs, ElasticClient client = null, Action progressReport = null)
        {
            if (sDocs == null)
                return;
            if (sDocs.Count() == 0)
                return;

            if (client == null)
                client = await Manager.GetESClient_InsolvenceDocsAsync();

            IEnumerable<SearchableDocument[]> chunks = sDocs.Chunk(100);

            foreach (SearchableDocument[] chunk in chunks)
            {
                if (progressReport != null)
                    progressReport();

                var res = await client.IndexManyAsync<SearchableDocument>(chunk);
                if (!res.IsValid)
                {
                    System.Threading.Thread.Sleep(100);
                    res = await client.IndexManyAsync<SearchableDocument>(chunk);
                    if (!res.IsValid)
                    {
                        throw new ApplicationException($"Inserting records {string.Join(";", chunk.Select(m => m.RecordId))} err :" + res.ServerError?.ToString());
                    }
                }

            }


        }
        public static async Task SaveAsync(SearchableDocument sDoc, ElasticClient client = null)
        {
            if (client == null)
                client = await Manager.GetESClient_InsolvenceDocsAsync();


            var res = await client.IndexAsync<SearchableDocument>(sDoc,
                o => o.Id(sDoc.RecordId)); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                System.Threading.Thread.Sleep(100);
                res = await client.IndexAsync<SearchableDocument>(sDoc,
                            o => o.Id(sDoc.RecordId)); //druhy parametr musi byt pole, ktere je unikatni

                if (!res.IsValid)
                {
                    throw new ApplicationException(sDoc.SpisovaZnacka + "  err :" + res.ServerError?.ToString());

                }
            }
        }
    }

}