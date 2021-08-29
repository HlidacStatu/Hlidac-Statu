using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

using Nest;

namespace HlidacStatu.Repositories
{
    public static class FirmaInElasticRepo
    {
        public static void Save(FirmaInElastic firmaInElastic)
        {
            ElasticClient c = Manager.GetESClient_Firmy();
            var res = c.Index<FirmaInElastic>(firmaInElastic, m => m.Id(firmaInElastic.Ico));
        }

    }
}