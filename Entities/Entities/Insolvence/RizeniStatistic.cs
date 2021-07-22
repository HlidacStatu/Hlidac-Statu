using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Insolvence
{
    public class RizeniStatistic
    {
        public RizeniStatistic()
        {
        }
        // public RizeniStatistic(string spisovaZnacka, IEnumerable<string> filterDluzniciFromThisList = null)
        //     : this(InsolvenceRepo.LoadFromES(spisovaZnacka, false, false)?.Rizeni, filterDluzniciFromThisList)
        // {
        // }

        public RizeniStatistic(Rizeni rizeni, IEnumerable<string> filterDluzniciFromThisList = null)
        {
            if (rizeni == null)
                throw new ArgumentNullException("rizeni");
            SpisovaZnacka = rizeni.SpisovaZnacka;
            Zahajeni = rizeni.DatumZalozeni ?? new DateTime(1990, 1, 1);
            PosledniUpdate = rizeni.PosledniZmena;
            SpravciCount = rizeni.Spravci?.Count() ?? 0;
            DluzniciCount = rizeni.Dluznici?.Count() ?? 0;
            VeriteleCount = rizeni.Veritele?.Count() ?? 0;
            DokumentyCount = rizeni.Dokumenty?.Count() ?? 0;
            Stav = rizeni.StavRizeni();
            VybraniDluznici = rizeni.Dluznici
                .Where(m => filterDluzniciFromThisList == null || filterDluzniciFromThisList?.Contains(m.ICO) == true)
                .Select(m => m.ICO)
                .ToArray();
        }
        public string SpisovaZnacka { get; set; }
        public string Stav { get; set; }
        public DateTime Zahajeni { get; set; }
        public DateTime PosledniUpdate { get; set; }
        public TimeSpan DelkaRizeni { get { return PosledniUpdate - Zahajeni; } }

        public int DokumentyCount { get; set; }

        public int VeriteleCount { get; set; }
        public int DluzniciCount { get; set; }
        public int SpravciCount { get; set; }

        public string[] VybraniDluznici { get; set; }
    }
}
