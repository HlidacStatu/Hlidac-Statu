using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories;

public static class DocumentHistoryRepo<T> where T: IDocumentHash
{
    public static async Task SaveAsync(T document, string dataSource, string originalId)
    {
        var elasticClient = await Manager.GetESClient_DocumentHistoryAsync();

        var documentHistory = new DocumentHistory<T>()
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

        await elasticClient.IndexDocumentAsync<DocumentHistory<T>>(documentHistory);
    }


    private static async Task<bool> Exists(ElasticClient elasticClient, DocumentHistory<T> documentHistory)
    {
        var res = await elasticClient.SearchAsync<DocumentHistory<T>>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(bm =>
                        bm.Term(t => t.Field(f => f.DocumentId).Value(documentHistory.DocumentId)) &&
                        bm.Term(t => t.Field(f => f.DocumentHash).Value(documentHistory.DocumentHash)) &&
                        bm.Term(t => t.Field(f => f.DocumentType).Value(documentHistory.DocumentType))
                    ))));
        return res.IsValid && res.Documents.Any();
    }
    
    public static async Task<List<DocumentHistory<T>>> LoadDocsWithId(string docId)
    {
        var elasticClient = await Manager.GetESClient_DocumentHistoryAsync();
        var res = await elasticClient.SearchAsync<DocumentHistory<T>>(s => s
            .Query(q => q
                .Bool(b => b
                    .Must(bm =>
                        bm.Term(t => t.Field(f => f.DocumentId).Value(docId)) &&
                        bm.Term(t => t.Field(f => f.DocumentType).Value(typeof(T).Name))
                    ))));


        return res.IsValid ? res.Documents.ToList() : default;
    }
}