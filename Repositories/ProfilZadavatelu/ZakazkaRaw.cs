using HlidacStatu.Connectors.External.ProfilZadavatelu;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;

using Nest;

using System;
using System.ComponentModel;
using System.Linq;

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

        public void Save(bool compare = true, ElasticClient client = null)
        {
            bool save = false;
            var latest = GetLatest(ZakazkaId);
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
                var es = (client ?? Manager.GetESClient_VerejneZakazkyNaProfiluRaw())
                    .IndexDocument<ZakazkaRaw>(this);
            }
        }


        public static ZakazkaRaw GetLatest(string zakazkaId, ElasticClient client = null)
        {
            client = client ?? Manager.GetESClient_VerejneZakazkyNaProfiluRaw();

            var res = client.Search<ZakazkaRaw>(s => s
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
