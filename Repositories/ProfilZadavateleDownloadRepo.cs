using HlidacStatu.Entities.Logs;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class ProfilZadavateleDownloadRepo
    {
        public static void Save(ProfilZadavateleDownload profilZadavatele, ElasticClient client = null)
        {
            var es = (client ?? Manager.GetESClient_Logs())
                .IndexDocument<ProfilZadavateleDownload>(profilZadavatele);
        }

    }
}