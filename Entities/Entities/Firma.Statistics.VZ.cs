using HlidacStatu.Lib.Analytics;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public partial class Statistics
        {
            public partial class VZ : CoreStat, IAddable<VZ>
            {
                public long PocetVypsanychVZ { get; set; } = 0;
                public decimal PocetUcastiVeVZ { get; set; } = 0;
                public long PocetVyherVeVZ { get; set; }
                public decimal CelkovaHodnotaVypsanychVZ { get; set; } = 0;
                public decimal CelkoveHodnotaVZsUcasti { get; set; }

                public VZ Add(VZ other)
                {
                    return new VZ()
                    {
                        PocetUcastiVeVZ = PocetUcastiVeVZ + (other?.PocetUcastiVeVZ ?? 0),
                        PocetVyherVeVZ = PocetVyherVeVZ + (other?.PocetVyherVeVZ ?? 0),
                        PocetVypsanychVZ = PocetVypsanychVZ + (other?.PocetVypsanychVZ ?? 0),
                        CelkovaHodnotaVypsanychVZ = CelkovaHodnotaVypsanychVZ + (other?.CelkovaHodnotaVypsanychVZ ?? 0),
                        CelkoveHodnotaVZsUcasti = CelkoveHodnotaVZsUcasti + (other?.CelkoveHodnotaVZsUcasti ?? 0),
                    };
                }
                public VZ Subtract(VZ other)
                {
                    return new VZ()
                    {
                        PocetUcastiVeVZ = PocetUcastiVeVZ - (other?.PocetUcastiVeVZ ?? 0),
                        PocetVyherVeVZ = PocetVyherVeVZ - (other?.PocetVyherVeVZ ?? 0),
                        PocetVypsanychVZ = PocetVypsanychVZ - (other?.PocetVypsanychVZ ?? 0),
                        CelkovaHodnotaVypsanychVZ = CelkovaHodnotaVypsanychVZ - (other?.CelkovaHodnotaVypsanychVZ ?? 0),
                        CelkoveHodnotaVZsUcasti = CelkoveHodnotaVZsUcasti - (other?.CelkoveHodnotaVZsUcasti ?? 0),
                    };
                }

                public override int NewSeasonStartMonth()
                {
                    return 4;
                }

                public override int UsualFirstYear()
                {
                    return 2010;
                }
            }
        }
    }
}

