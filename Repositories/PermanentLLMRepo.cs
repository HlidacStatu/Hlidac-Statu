using HlidacStatu.Entities.PermanentLLM;
using Nest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class PermanentLLMRepo

    {

        public static async Task SaveAsync<T>(T item)
            where T : BaseItem
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.IndexAsync(item, m => m.Id(item.Id));
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }

        }


        public static async Task<T[]> SearchAsync<T>(
            string queryString, int count)
            where T : BaseItem
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.SearchAsync<T>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query(queryString)
                        .DefaultOperator(Operator.And)
                        )
                    )
                    .Size(count)
                );
            if (res.IsValid == false)
                throw new ApplicationException(res.ServerError?.ToString());

            return res.Hits.Select(m => m.Source).ToArray();
        }

        public static async Task<T[]> SearchPerKeysAsync<T>(
            string documentType, string documentId, string partType, string fileId = null)
            where T : BaseItem
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.SearchAsync<T>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query(
                            $"documentType:{documentType} AND documentID:{documentId}"
                            + $" {(string.IsNullOrEmpty(fileId) ? "" : $"AND fileID:{fileId}")}"
                            + $" {(string.IsNullOrEmpty(partType) ? "" : $"AND partType:{partType}")}"
                            )
                        .DefaultOperator(Operator.And)
                    )
                ));
            if (res.IsValid == false)
                throw new ApplicationException(res.ServerError?.ToString());

            return res.Hits.Select(m => m.Source).ToArray();
        }

        public static async Task<T> LoadAsync<T>(
            string documentType, string documentId, string partType, string fileId = null
            )
            where T : BaseItem
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.GetAsync<T>(Entities.PermanentLLM.BaseItem<T>
                .GetId(documentType, documentId, partType, fileId));
            if (!res.Found && res.ServerError != null)
                throw new ApplicationException(res.ServerError?.ToString());
            else if (res.Found && res.IsValid)
                return res.Source;
            else
                return null;
        }

        public static async Task<bool> _existsAsync(string documentType, string documentId, string partType, string fileId)
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.DocumentExistsAsync<object>(Entities.PermanentLLM.BaseItem
                .GetId(documentType, documentId, partType, fileId));
            if (!res.IsValid && res.ServerError != null)
                throw new ApplicationException(res.ServerError?.ToString());
            else
                return res.Exists;
        }
        public static async Task<bool> DeleteAsync(string documentId)
        {

            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var deleted = await dbSP.DeleteAsync<object>(documentId);
            return deleted.IsValid;
        }
        public static async Task<long> DeleteAsync(string documentType, string documentId, string partType, string fileId = null)
        {

            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var deleted = await dbSP.DeleteByQueryAsync<object>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query(
                            $"documentType:{documentType} AND documentID:{documentId}"
                            + $" {(string.IsNullOrEmpty(fileId) ? "" : $"AND fileID:{fileId}")}"
                            + $" {(string.IsNullOrEmpty(partType) ? "" : $"AND partType:{partType}")}"
                            )
                        .DefaultOperator(Operator.And)
                    )
                )
            );
            if (deleted.IsValid == false)
                return 0;

            return deleted.Deleted;
        }

        public static async Task<bool> ExistsAsync(string documentType, string documentId, string partType, string fileId = null)
        {
            if (
                documentType == HlidacStatu.Entities.PermanentLLM.FullSummary.DOCUMENTTYPE
                && !string.IsNullOrEmpty(fileId)
                )
                return await _existsAsync(documentType, documentId, partType, fileId);


            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.SearchAsync<object>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query(
                            $"documentType:{documentType} AND documentID:{documentId}"
                            + $" {(string.IsNullOrEmpty(fileId) ? "" : $"AND fileID:{fileId}")}"
                            + $" {(string.IsNullOrEmpty(partType) ? "" : $"AND partType:{partType}")}"
                        )
                        .DefaultOperator(Operator.And)
                    )
                )
                    .Size(0)
                    .Source(false)
                );
            if (res.IsValid == false && res.ServerError != null)
                throw new ApplicationException(res.ServerError?.ToString());

            return res.Total > 0;
        }

    }
}