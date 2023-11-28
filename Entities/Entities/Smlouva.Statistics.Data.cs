using HlidacStatu.Lib.Analytics;

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace HlidacStatu.Entities
{
    public partial class Smlouva
    {
        public partial class Statistics
        {
            public class Data : CoreStat, IAddable<Data>
            {
                public long PocetSmluv { get; set; } = 0;
                public decimal CelkovaHodnotaSmluv { get; set; } = 0;
                public long PocetSmluvSeSoukromymSubj { get; set; }
                public decimal CelkovaHodnotaSmluvSeSoukrSubj { get; set; } = 0;
                public long PocetSmluvBezCenySeSoukrSubj { get; set; }

                public long PocetSmluvBezCeny { get; set; } = 0;
                public long PocetSmluvBezSmluvniStrany { get; set; } = 0;
                public decimal SumKcSmluvBezSmluvniStrany { get; set; } = 0;

                public long PocetSmluvSponzorujiciFirmy { get; set; } = 0;
                public long PocetSmluvBezCenySponzorujiciFirmy { get; set; }
                public decimal SumKcSmluvSponzorujiciFirmy { get; set; } = 0;

                public long PocetSmluvSeZasadnimNedostatkem { get; set; }
                public long PocetSmluvULimitu { get; set; }
                public long PocetSmluvOVikendu { get; set; }
                public long PocetZacernenychSmluv { get; set; }
                public long PocetSmluvNovaFirma { get; set; }

                public Dictionary<int, SimpleStat> PoOblastech { get; set; } = new Dictionary<int, SimpleStat>();

                [JsonIgnore]
                public decimal PercentSmluvBezCeny => (PocetSmluv == 0 ? 0 : (decimal)PocetSmluvBezCeny / (decimal)PocetSmluv);
                [JsonIgnore]
                public decimal PercentSmluvBezSmluvniStrany => (PocetSmluv == 0 ? 0 : (decimal)PocetSmluvBezSmluvniStrany / (decimal)PocetSmluv);
                [JsonIgnore]
                public decimal PercentKcBezSmluvniStrany => (CelkovaHodnotaSmluv == 0 ? 0 : (decimal)SumKcSmluvBezSmluvniStrany / (decimal)CelkovaHodnotaSmluv);
                [JsonIgnore]
                public decimal PercentSmluvPolitiky => (PocetSmluv == 0 ? 0 : (decimal)PocetSmluvSponzorujiciFirmy / (decimal)PocetSmluv);
                [JsonIgnore]
                public decimal PercentKcSmluvPolitiky => (CelkovaHodnotaSmluv == 0 ? 0 : (decimal)SumKcSmluvSponzorujiciFirmy / (decimal)CelkovaHodnotaSmluv);
                [JsonIgnore]
                public decimal PercentSmluvULimitu => (PocetSmluv == 0 ? 0 : (decimal)PocetSmluvULimitu / (decimal)PocetSmluv);
                [JsonIgnore]
                public decimal PercentSmluvSeZasadnimNedostatkem => (PocetSmluv == 0 ? 0 : (decimal)PocetSmluvSeZasadnimNedostatkem / (decimal)PocetSmluv);

                public Data Add(Data other)
                {
                    var d = new Data()
                    {
                        PocetSmluv = PocetSmluv + (other?.PocetSmluv ?? 0),
                        CelkovaHodnotaSmluv = CelkovaHodnotaSmluv + (other?.CelkovaHodnotaSmluv ?? 0),
                        PocetSmluvSeSoukromymSubj = PocetSmluvSeSoukromymSubj + (other?.PocetSmluvSeSoukromymSubj ?? 0),
                        CelkovaHodnotaSmluvSeSoukrSubj = CelkovaHodnotaSmluvSeSoukrSubj + (other?.CelkovaHodnotaSmluvSeSoukrSubj ?? 0),
                        PocetSmluvBezCenySeSoukrSubj = PocetSmluvBezCenySeSoukrSubj + (other?.PocetSmluvBezCenySeSoukrSubj ?? 0),
                        PocetSmluvBezCeny = PocetSmluvBezCeny + (other?.PocetSmluvBezCeny ?? 0),
                        PocetSmluvBezSmluvniStrany = PocetSmluvBezSmluvniStrany + (other?.PocetSmluvBezSmluvniStrany ?? 0),
                        SumKcSmluvBezSmluvniStrany = SumKcSmluvBezSmluvniStrany + (other?.SumKcSmluvBezSmluvniStrany ?? 0),
                        PocetSmluvSponzorujiciFirmy = PocetSmluvSponzorujiciFirmy + (other?.PocetSmluvSponzorujiciFirmy ?? 0),
                        SumKcSmluvSponzorujiciFirmy = SumKcSmluvSponzorujiciFirmy + (other?.SumKcSmluvSponzorujiciFirmy ?? 0),
                        PocetSmluvSeZasadnimNedostatkem = PocetSmluvSeZasadnimNedostatkem + (other?.PocetSmluvSeZasadnimNedostatkem ?? 0),
                        PocetSmluvULimitu = PocetSmluvULimitu + (other?.PocetSmluvULimitu ?? 0),
                        PocetSmluvOVikendu = PocetSmluvOVikendu + (other?.PocetSmluvOVikendu ?? 0),
                        PocetZacernenychSmluv = PocetZacernenychSmluv + (other?.PocetZacernenychSmluv ?? 0),
                        PocetSmluvNovaFirma = PocetSmluvNovaFirma + (other?.PocetSmluvNovaFirma ?? 0),
                        PoOblastech = PoOblastech.ToDictionary(entry => entry.Key,
                            entry => entry.Value)
                    };

                    if (other.PoOblastech != null)
                        foreach (var o in other.PoOblastech)
                        {
                            if (d.PoOblastech.ContainsKey(o.Key))
                            {
                                d.PoOblastech[o.Key].Pocet = d.PoOblastech[o.Key].Pocet + o.Value.Pocet;
                                d.PoOblastech[o.Key].CelkemCena = d.PoOblastech[o.Key].CelkemCena + o.Value.CelkemCena;
                            }
                            else
                                d.PoOblastech.Add(o.Key, o.Value);
                        }

                    return d;
                }
                public Data Subtract(Data other)
                {
                    var d = new Data()
                    {
                        PocetSmluv = PocetSmluv - (other?.PocetSmluv ?? 0),
                        CelkovaHodnotaSmluv = CelkovaHodnotaSmluv - (other?.CelkovaHodnotaSmluv ?? 0),
                        PocetSmluvSeSoukromymSubj = PocetSmluvSeSoukromymSubj - (other?.PocetSmluvSeSoukromymSubj ?? 0),
                        CelkovaHodnotaSmluvSeSoukrSubj = CelkovaHodnotaSmluvSeSoukrSubj - (other?.CelkovaHodnotaSmluvSeSoukrSubj ?? 0),
                        PocetSmluvBezCenySeSoukrSubj = PocetSmluvBezCenySeSoukrSubj - (other?.PocetSmluvBezCenySeSoukrSubj ?? 0),
                        PocetSmluvBezCeny = PocetSmluvBezCeny - (other?.PocetSmluvBezCeny ?? 0),
                        PocetSmluvBezSmluvniStrany = PocetSmluvBezSmluvniStrany - (other?.PocetSmluvBezSmluvniStrany ?? 0),
                        SumKcSmluvBezSmluvniStrany = SumKcSmluvBezSmluvniStrany - (other?.SumKcSmluvBezSmluvniStrany ?? 0),
                        PocetSmluvSponzorujiciFirmy = PocetSmluvSponzorujiciFirmy - (other?.PocetSmluvSponzorujiciFirmy ?? 0),
                        SumKcSmluvSponzorujiciFirmy = SumKcSmluvSponzorujiciFirmy - (other?.SumKcSmluvSponzorujiciFirmy ?? 0),
                        PocetSmluvSeZasadnimNedostatkem = PocetSmluvSeZasadnimNedostatkem - (other?.PocetSmluvSeZasadnimNedostatkem ?? 0),
                        PocetSmluvULimitu = PocetSmluvULimitu - (other?.PocetSmluvULimitu ?? 0),
                        PocetSmluvOVikendu = PocetSmluvOVikendu - (other?.PocetSmluvOVikendu ?? 0),
                        PocetZacernenychSmluv = PocetZacernenychSmluv - (other?.PocetZacernenychSmluv ?? 0),
                        PocetSmluvNovaFirma = PocetSmluvNovaFirma - (other?.PocetSmluvNovaFirma ?? 0),
                        PoOblastech = PoOblastech.ToDictionary(entry => entry.Key,
                            entry => entry.Value)
                    };

                    if (other.PoOblastech != null)
                        foreach (var o in other.PoOblastech)
                        {
                            if (d.PoOblastech.ContainsKey(o.Key))
                            {
                                d.PoOblastech[o.Key].Pocet = d.PoOblastech[o.Key].Pocet - o.Value.Pocet;
                                d.PoOblastech[o.Key].CelkemCena = d.PoOblastech[o.Key].CelkemCena - o.Value.CelkemCena;
                            }
                            //else
                            //    d.PoOblastech.Add(o.Key, o.Value);
                        }

                    return d;
                }
                public override int NewSeasonStartMonth()
                {
                    return 4;
                }

                public override int UsualFirstYear()
                {
                    return 2016;
                }
            }
        }

    }
}
