using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories;

public static class DocumentHistoryRepo
{
    public static async Task SaveAsync<T>(T document, string dataSource, string originalId) where T : IDocumentHash
    {
        var elasticClient = await Manager.GetESClient_DocumentHistoryAsync();

        var documentHistory = new DocumentHistory()
        {
            SaveDate = DateTime.Now,
            Document = document,
            DataSource = dataSource,
            DocumentId = originalId,
            DocumentHash = document.GetDocumentHash(),
            DocumentType = typeof(T).Name

        };
        
        // check if the exactly same document is already stored
        if(await Exists(elasticClient, documentHistory))
            return;

        await elasticClient.IndexDocumentAsync<DocumentHistory>(documentHistory);
    }


    private static async Task<bool> Exists(ElasticClient elasticClient, DocumentHistory documentHistory)
    {
        var res = await elasticClient.SearchAsync<DocumentHistory>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(bm =>
                        bm.Term(t => t.Field(f => f.DocumentId).Value(documentHistory.DocumentId)) &&
                        bm.Term(t => t.Field(f => f.DocumentHash).Value(documentHistory.DocumentHash)) &&
                        bm.Term(t => t.Field(f => f.DocumentType).Value(documentHistory.DocumentType))
                    ))));
        return res.IsValid && res.Documents.Any();
    }
}