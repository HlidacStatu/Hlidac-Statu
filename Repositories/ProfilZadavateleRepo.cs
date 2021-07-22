using System;
using System.Linq;
using System.Security.Policy;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class ProfilZadavateleRepo
    {
        public static void Save(ProfilZadavatele profilZadavatele, ElasticClient client = null)
        {
            if (
                !string.IsNullOrEmpty(profilZadavatele.EvidencniCisloFormulare)
                && !string.IsNullOrEmpty(profilZadavatele.EvidencniCisloFormulare)
                && !string.IsNullOrEmpty(profilZadavatele.Url)
            )
            {
                var es = (client ?? Manager.GetESClient_ProfilZadavatele())
                    .IndexDocument<ProfilZadavatele>(profilZadavatele);
            }
        }
        
        public static ProfilZadavatele GetByUrl(string url, ElasticClient client = null)
        {
            var f = (client ?? Manager.GetESClient_ProfilZadavatele())
                .Search<ProfilZadavatele>(s => s
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

        public static ProfilZadavatele GetById(string profileId, ElasticClient client = null)
        {
            if (string.IsNullOrEmpty(profileId))
                return null;
            var f = (client ?? Manager.GetESClient_ProfilZadavatele())
                .Get<ProfilZadavatele>(profileId);
            if (f.Found)
                return f.Source;
            else
                return null;
        }

        public static ProfilZadavatele[] GetByIco(string ico, ElasticClient client = null)
        {
            try
            {
                var f = (client ?? Manager.GetESClient_ProfilZadavatele())
                    .Search<ProfilZadavatele>(s => s
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

        public static ProfilZadavatele GetByRawId(string datasetname, string profileId, ElasticClient client = null)
        {
            var id = datasetname + "-" + profileId;
            return GetById(id, client);
        }
    }
}