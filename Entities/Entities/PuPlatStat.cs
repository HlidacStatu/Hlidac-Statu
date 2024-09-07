using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Entities;

public class PuPlatStat
{
    public IEnumerable<PuPlat> Platy { get; }

    public PuPlatStat(IEnumerable<PuPlat> platy)
    {
        if (platy == null)
            Platy = Array.Empty<PuPlat>();

        Platy = platy;

        Pocet = Platy.Count();
        if (Pocet > 0)
        {
            Stats = Platy
                .GroupBy(k => k.Rok, (k, v) => new KeyValuePair<int, Stat>(k, new Stat(v.Where(m => m.Rok == k))))
                .ToDictionary(k => k.Key, v => v.Value);

        }
    }

    public bool HasPlaty => Pocet > 0;
    public int Pocet { get; private set; }

    public Dictionary<int, Stat> Stats { get; private set; } = new Dictionary<int, Stat>();

    public class Stat
    {
        public Stat(IEnumerable<PuPlat> platy)
        {
            var data = platy.Select(m => m.HrubyMesicniPlatVcetneOdmen);
            Pocet = data.Count();
            Percentil10 = HlidacStatu.Util.MathTools.PercentileCont(0.10m, data);
            Percentil25 = HlidacStatu.Util.MathTools.PercentileCont(0.25m, data);
            Percentil50 = HlidacStatu.Util.MathTools.PercentileCont(0.50m, data);
            Percentil75 = HlidacStatu.Util.MathTools.PercentileCont(0.75m, data);
            Percentil90 = HlidacStatu.Util.MathTools.PercentileCont(0.90m, data);
            Min = data.Min();
            Max = data.Max();
            Prumer = data.Average();

        }
        public int Pocet { get; private set; }
        public decimal Percentil10 { get; private set; }
        public decimal Percentil25 { get; private set; }
        public decimal Percentil50 { get; private set; }
        public decimal Percentil75 { get; private set; }
        public decimal Percentil90 { get; private set; }
        public decimal Prumer { get; private set; }
        public decimal Min { get; private set; }
        public decimal Max { get; private set; }
    }
}