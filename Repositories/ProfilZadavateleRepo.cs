using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;

using Nest;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class ProfilZadavateleRepo
    {
        public static async Task SaveAsync(ProfilZadavatele profilZadavatele, ElasticClient client = null)
        {
            if (
                !string.IsNullOrEmpty(profilZadavatele.EvidencniCisloFormulare)
                && !string.IsNullOrEmpty(profilZadavatele.EvidencniCisloFormulare)
                && !string.IsNullOrEmpty(profilZadavatele.Url)
            )
            {
                await (client ?? Manager.GetESClient_ProfilZadavatele())
                    .IndexDocumentAsync<ProfilZadavatele>(profilZadavatele);
            }
        }

        public static async Task<ProfilZadavatele> GetByUrlAsync(string url, ElasticClient client = null)
        {
            var f = await (client ?? Manager.GetESClient_ProfilZadavatele())
                .SearchAsync<ProfilZadavatele>(s => s
                    .Query(q => q
                        .Term(t => t.Field(ff => ff.Url).Value(url))
                    )
                );
            if (f.IsValid)
            {
                if (f.Total == 0)
                    return null;
                else
                    return f.Hits.First()?.Source;
            }
            else
                throw new ApplicationException("ES error\n\n" + f.ServerError.ToString());
        }

        public static async Task<ProfilZadavatele> GetByIdAsync(string profileId, ElasticClient client = null)
        {
            if (string.IsNullOrEmpty(profileId))
                return null;
            var f = await (client ?? Manager.GetESClient_ProfilZadavatele())
                .GetAsync<ProfilZadavatele>(profileId);
            if (f.Found)
                return f.Source;
            else
                return null;
        }

        public static async Task<ProfilZadavatele[]> GetByIcoAsync(string ico, ElasticClient client = null)
        {
            try
            {
                var f = await (client ?? Manager.GetESClient_ProfilZadavatele())
                    .SearchAsync<ProfilZadavatele>(s => s
                        .Query(q => q
                            .Term(t => t.Field(ff => ff.Zadavatel.ICO).Value(ico))
                        )
                    );
                if (f.IsValid)
                    return f.Hits.Select(m => m.Source).ToArray();
                else
                    return new ProfilZadavatele[] { };
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error("ERROR ProfilZadavatele.GetByIco for ICO " + ico, e);
                return new ProfilZadavatele[] { };
            }
        }

        public static Task<ProfilZadavatele> GetByRawIdAsync(string datasetname, string profileId, ElasticClient client = null)
        {
            var id = datasetname + "-" + profileId;
            return GetByIdAsync(id, client);
        }
    }
}