using HlidacStatu.Connectors.External.ProfilZadavatelu;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;

using Nest;

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.ProfilZadavatelu
{
    [Serializable()]
    public class ZakazkaRaw
    {

        public ZakazkaRaw() { }
        public ZakazkaRaw(ZakazkaStructure zakazkaStr, ProfilZadavatele profil)
        {
            Profil = profil.Id;
            ZakazkaNaProfilu = zakazkaStr;
            ZakazkaId = profil.Id + "_" + zakazkaStr.VZ.kod_vz_na_profilu.Value;

        }

        [Keyword()]
        [Description("")]
        public string ZakazkaId { get; set; }
        [Keyword()]
        [Description("ID profilu")]
        public string Profil { get; set; }
        public DateTime LastUpdate { get; set; }

        public int? Converted { get; set; } = null;

        public ZakazkaStructure ZakazkaNaProfilu { get; set; }

        public async Task SaveAsync(bool compare = true, ElasticClient client = null)
        {
            bool save = false;
            var latest = GetLatestAsync(ZakazkaId);
            if (compare)
            {
                if (!Validators.AreObjectsEqual(this, latest, false, "LastUpdate"))
                    save = true;
            }
            else
                save = true;


            if (save)
            {
                LastUpdate = DateTime.Now;
                var cli = client ?? await Manager.GetESClient_VerejneZakazkyNaProfiluRawAsync();
                var es = await (client ?? cli).IndexDocumentAsync<ZakazkaRaw>(this);
            }
        }


        public static async Task<ZakazkaRaw> GetLatestAsync(string zakazkaId, ElasticClient client = null)
        {
            client = client ?? await Manager.GetESClient_VerejneZakazkyNaProfiluRawAsync();

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
}
