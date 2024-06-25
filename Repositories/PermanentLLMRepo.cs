using Nest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class PermanentLLMRepo<T>
        where T : class
    {

        public static async Task SaveAsync(Entities.PermanentLLM.BaseItem<T> item)
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.IndexAsync(item, m => m.Id(item.Id));
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }

        }

        public static async Task<Entities.PermanentLLM.BaseItem<T>[]> SearchPerKeysAsync(string documentType, string documentId, string fileId = null)
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.SearchAsync<Entities.PermanentLLM.BaseItem<T>>(s => s
                    .Query(q => q
                        .QueryString(qs => qs
                        .Query($"documentType:{documentType} AND documentID:{documentId} {(string.IsNullOrEmpty(fileId) ? "" : $"AND fileID:{fileId}")}")
                        .DefaultOperator(Operator.And)
                    )
                ));
            if (res.IsValid == false)
                throw new ApplicationException(res.ServerError?.ToString());

            return res.Hits.Select(m => m.Source).ToArray();
        }

        public static async Task<Entities.PermanentLLM.BaseItem<T>> LoadAsync(string documentType, string documentId, string fileId = null)
        {
            var dbSP = await HlidacStatu.Connectors.Manager.GetESClient_PermanentLLMAsync();

            var res = await dbSP.GetAsync<Entities.PermanentLLM.BaseItem<T>>(Entities.PermanentLLM.BaseItem<T>.GetId(documentType, documentId, fileId));
            if (!res.Found && res.ServerError != null)
                throw new ApplicationException(res.ServerError?.ToString());
            else if (res.Found && res.IsValid)
                return res.Source;
            else
                return null;
        }



    }
}