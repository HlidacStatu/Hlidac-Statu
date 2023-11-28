using HlidacStatu.Connectors;
using HlidacStatu.MLUtil.Splitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class SplitSmlouvaRepo
    {
        public static async Task<SplitSmlouva> LoadAsync(string smlouvaId, string prilohaId)
        {
            Nest.ElasticClient client = await Manager.GetESClient_SplitSmlouvyAsync();

            var res = await client.GetAsync<SplitSmlouva>(SplitSmlouva.GetId(smlouvaId, prilohaId));
            if (res.IsValid && res.Found)
                return res.Source;
            else
                return null;
        }
        public static async Task SaveAsync(SplitSmlouva item)
        {

            try
            {
                var client = await Manager.GetESClient_SplitSmlouvyAsync();
                await client.IndexAsync<SplitSmlouva>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("SplitSmlouvaRepo.Save error ", e);
                throw;
            }
        }
    }
}
