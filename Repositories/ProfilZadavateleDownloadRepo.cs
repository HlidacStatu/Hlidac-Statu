using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities.Logs;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class ProfilZadavateleDownloadRepo
    {
        public static async Task SaveAsync(ProfilZadavateleDownload profilZadavatele, ElasticClient client = null)
        {
            var es = await (client ?? Manager.GetESClient_Logs())
                .IndexDocumentAsync<ProfilZadavateleDownload>(profilZadavatele);
        }

    }
}