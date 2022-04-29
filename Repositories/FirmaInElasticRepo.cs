using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using Nest;

namespace HlidacStatu.Repositories
{
    public static class FirmaInElasticRepo
    {
        public static async Task SaveAsync(FirmaInElastic firmaInElastic)
        {
            ElasticClient c = Manager.GetESClient_Firmy();
            var res = await c.IndexAsync<FirmaInElastic>(firmaInElastic, m => m.Id(firmaInElastic.Ico));
        }

    }
}