using HlidacStatu.Lib.Analytics;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public partial class Statistics
        {
            public partial class VZ : CoreStat, IAddable<VZ>
            {
                public long PocetJakoDodavatel { get; set; } = 0;
                public long PocetJakoZadavatel { get; set; } = 0;
                public decimal CelkovaHodnotaJakoDodavatel { get; set; } = 0m;
                public decimal CelkovaHodnotaJakoZadavatel { get; set; } = 0m;

                public VZ Add(VZ other)
                {
                    return new VZ()
                    {
                        PocetJakoDodavatel = PocetJakoDodavatel + (other?.PocetJakoDodavatel ?? 0),
                        PocetJakoZadavatel = PocetJakoZadavatel + (other?.PocetJakoZadavatel ?? 0),
                        CelkovaHodnotaJakoDodavatel = CelkovaHodnotaJakoDodavatel + (other?.CelkovaHodnotaJakoDodavatel ?? 0),
                        CelkovaHodnotaJakoZadavatel = CelkovaHodnotaJakoZadavatel + (other?.CelkovaHodnotaJakoZadavatel ?? 0),
                    };
                }
                public VZ Subtract(VZ other)
                {
                    return new VZ()
                    {
                        PocetJakoDodavatel = PocetJakoDodavatel - (other?.PocetJakoDodavatel ?? 0),
                        PocetJakoZadavatel = PocetJakoZadavatel - (other?.PocetJakoZadavatel ?? 0),
                        CelkovaHodnotaJakoDodavatel = CelkovaHodnotaJakoDodavatel - (other?.CelkovaHodnotaJakoDodavatel ?? 0),
                        CelkovaHodnotaJakoZadavatel = CelkovaHodnotaJakoZadavatel - (other?.CelkovaHodnotaJakoZadavatel ?? 0),
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

