using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Datasets.TransparentniUcty
{
    public class BankovniPolozka : IEqualityComparer<BankovniPolozka>, Plugin.TransparetniUcty.IBankovniPolozka
    {
        private static DataSet _client =
            DataSet.CachedDatasets.Get("transparentni-ucty-transakce");

        public BankovniPolozka() { }
        public BankovniPolozka(Plugin.TransparetniUcty.IBankovniPolozka ip)
        {
            AddId = ip.AddId;
            Castka = ip.Castka;
            CisloProtiuctu = ip.CisloProtiuctu;
            CisloUctu = ip.CisloUctu;
            Datum = ip.Datum;
            Id = ip.Id;
            KS = ip.KS;
            NazevProtiuctu = ip.NazevProtiuctu;
            PopisTransakce = ip.PopisTransakce;
            SS = ip.SS;
            VS = ip.VS;
            ZdrojUrl = ip.ZdrojUrl;
            ZpravaProPrijemce = ip.ZpravaProPrijemce;

        }

        //idu,majitel,nazev,datum,protiucet,popis,valuta,typ,castka,poznamka
        public string Id { get; set; } = null;

        public void InitId()
        {
            string[] data = new string[] {
                    Castka.ToString(HlidacStatu.Util.Consts.czCulture),
                    CisloUctu,
                    CisloProtiuctu,
                    Datum.ToString("dd.MM.yyyy", HlidacStatu.Util.Consts.czCulture),
                    VS };

            Id = Devmasters.Crypto.Hash.ComputeHashToHex(data.Aggregate((f, s) => f + "|" + s));

        }

        private string _cisloUctu = string.Empty;
        public string CisloUctu
        {
            get
            {
                return _cisloUctu;
            }
            set
            {
                _cisloUctu = BankovniUcet.NormalizeCisloUctu(value);
            }
        }
        public DateTime Datum { get; set; }
        public string PopisTransakce { get; set; } = "";
        public string NazevProtiuctu { get; set; } = "";

        private string _cisloProtiUctu = string.Empty;

        public string CisloProtiuctu
        {
            get
            {
                return _cisloProtiUctu;
            }
            set
            {
                _cisloProtiUctu = BankovniUcet.NormalizeCisloUctu(value);
            }
        }

        public string ZpravaProPrijemce { get; set; } = "";

        public string VS { get; set; } = "";
        public string KS { get; set; } = "";
        public string SS { get; set; } = "";

        public decimal Castka { get; set; }

        public string AddId { get; set; } = "";

        public string ZdrojUrl { get; set; }

        private BankovniUcet _bu = null;
        public async Task<BankovniUcet> GetBankovniUcetAsync()
        {
            if (_bu == null)
                _bu = await BankovniUcet.GetAsync(CisloUctu);
            return _bu;
        }

        public Task<string> GetUrlAsync(bool local = true) => GetUrlAsync(local, false, string.Empty);
        public Task<string> GetUrlAsync(bool local, string foundWithQuery) => GetUrlAsync(local, false, "");

        public async Task<string> GetUrlAsync(bool local = true, bool onList = false, string foundWithQuery = "")
        {
            var bu = await GetBankovniUcetAsync(); 
            if (bu == null)
                return "";
            if (onList)
                return bu.GetUrl(local, foundWithQuery) + "#" + Id;
            else
            {
                string url = "/data/Detail/transparentni-ucty-transakce/" + System.Net.WebUtility.UrlEncode(Id);
                if (!string.IsNullOrEmpty(foundWithQuery))
                    url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

                if (!local)
                    return "https://www.hlidacstatu.cz" + url;
                return url;
            }
        }

        public bool EqualsBezProtiUctu(BankovniPolozka x, BankovniPolozka y)
        {
            return (
                x.Castka == y.Castka
                && x.CisloUctu == y.CisloUctu
                && (x.CisloProtiuctu == y.CisloProtiuctu || string.IsNullOrEmpty(x.CisloProtiuctu) || string.IsNullOrEmpty(y.CisloProtiuctu))
                && x.Datum == y.Datum
                && x.KS == y.KS
                && x.SS == y.SS
                && x.VS == y.VS
                && x.NazevProtiuctu == y.NazevProtiuctu
                && x.PopisTransakce == y.PopisTransakce
                && x.ZpravaProPrijemce == y.ZpravaProPrijemce
                );
        }

        public bool Equals(BankovniPolozka x, BankovniPolozka y)
        {
            return (
                x.Castka == y.Castka
                && x.CisloUctu == y.CisloUctu
                && x.CisloProtiuctu == y.CisloProtiuctu
                && x.Datum == y.Datum
                && x.KS == y.KS
                && x.SS == y.SS
                && x.VS == y.VS
                && x.NazevProtiuctu == y.NazevProtiuctu
                && x.PopisTransakce == y.PopisTransakce
                && x.ZpravaProPrijemce == y.ZpravaProPrijemce
                );
        }

        public int GetHashCode(BankovniPolozka obj)
        {
            //http://stackoverflow.com/a/4630550
            return
                new
                {
                    obj.Castka,
                    obj.CisloUctu,
                    obj.CisloProtiuctu,
                    obj.Datum,
                    obj.KS,
                    obj.NazevProtiuctu,
                    obj.PopisTransakce,
                    obj.SS,
                    obj.VS,
                    obj.ZpravaProPrijemce
                }.GetHashCode();
        }

        public async Task<bool> ExistsAsync()
        {
            if (string.IsNullOrEmpty(Id))
                InitId();

            return await _client.ItemExistsAsync(Id);
        }

        public async Task<bool> DeleteAsync()
        {
            return await _client.DeleteDataAsync(Id);
        }

        public async Task SaveAsync(string user, bool updateId = true)
        {
            if (updateId || string.IsNullOrEmpty(Id))
                InitId();

            await _client.AddDataAsync(this, Id, user);
        }


        public static async Task<BankovniPolozka> GetAsync(string transactionId)
        {
            BankovniPolozka bu = await _client.GetDataAsync<BankovniPolozka>(transactionId);
            return bu;
        }

        public static async Task<IEnumerable<BankovniPolozka>> GetAllAsync()
        {
            return await _client.GetAllDataAsync<BankovniPolozka>();
        }

    }
}
