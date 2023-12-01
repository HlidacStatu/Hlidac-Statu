using HlidacStatu.Connectors;
using HlidacStatu.MLUtil.Splitter;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public class SplitSmlouvaRepo
    {
        public static async Task<bool> ExistsAsync(string smlouvaId, string prilohaId)
        {
            Nest.ElasticClient client = await Manager.GetESClient_SplitSmlouvyAsync();

            string id = SplitSmlouva.GetId(smlouvaId, prilohaId);
            var res = await client.DocumentExistsAsync<SplitSmlouva>(id);
            if (res.IsValid && res.Exists)
                return true;
            else
                return false;
        }

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
                var res = await client.IndexAsync<SplitSmlouva>(item, m => m.Id(item.Id));

                if (res.IsValid == false)
                    HlidacStatu.Util.Consts.Logger.Error($"SplitSmlouvaRepo.Save error: {res.ServerError}",res.OriginalException );

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("SplitSmlouvaRepo.Save error ", e);
                throw;
            }
        }
    }
}
