using HlidacStatu.Entities.VZ;

using Nest;

using System;
using System.ComponentModel;
using HlidacStatu.Connectors.External.ProfilZadavatelu;


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

        

    }
}
