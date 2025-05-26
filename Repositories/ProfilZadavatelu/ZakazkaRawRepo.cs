using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using Nest;

namespace HlidacStatu.Repositories.ProfilZadavatelu;

public static class ZakazkaRawRepo
{
    public static async Task SaveAsync(this ZakazkaRaw zakazkaRaw, bool compare = true, ElasticClient client = null)
    {
        bool save = false;
        var latest = await GetLatestAsync(zakazkaRaw.ZakazkaId);
        if (compare)
        {
            if (!Validators.AreObjectsEqual(zakazkaRaw, latest, false, "LastUpdate"))
                save = true;
        }
        else
            save = true;


        if (save)
        {
            zakazkaRaw.LastUpdate = DateTime.Now;
            var cli = client ?? Manager.GetESClient_VerejneZakazkyNaProfiluRaw();
            var es = await (client ?? cli).IndexDocumentAsync<ZakazkaRaw>(zakazkaRaw);
        }
    }


    public static async Task<ZakazkaRaw> GetLatestAsync(string zakazkaId, ElasticClient client = null)
    {
        client = client ?? Manager.GetESClient_VerejneZakazkyNaProfiluRaw();

        var res = await client.SearchAsync<ZakazkaRaw>(s => s
            .Query(q => q
                .Term(t => t.Field(f => f.ZakazkaId).Value(zakazkaId))
            )
            .Sort(ss => ss.Descending(ff => ff.LastUpdate))
            .Size(1)
        );

        if (res.Total == 0)
            return null;
        else
            return res.Hits.First().Source;

    }
}