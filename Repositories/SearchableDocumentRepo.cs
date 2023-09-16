using HlidacStatu.Connectors;
using HlidacStatu.Entities.Insolvence;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class SearchableDocumentRepo
    {

        public static async Task<bool> Exist(string spisovaZnacka)
        {

            var client = await Manager.GetESClient_InsolvenceDocsAsync();

            var res = await client.SearchAsync<SearchableDocument>(s => s
                    .Query(q => q
                        .QueryString(qs => qs.Query($"spisovaZnacka:\"{spisovaZnacka}\" AND internalTopHit:1")
                    )
                )
            );
            if (res.IsValid)
                return res.Total > 0;
            else
                return false;

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