using HlidacStatu.Lib.Analytics;
using System;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public partial class Statistics
        {

            public class Dotace : CoreStat, IAddable<Dotace>
            {
                public int PocetDotaci { get; set; } = 0;
                public int PocetCerpani { get; set; } = 0;
                public decimal CelkemCerpano { get; set; } = 0;

                

                public Dotace Add(Dotace other)
                {
                    return new Dotace()
                    {
                        CelkemCerpano = CelkemCerpano + (other?.CelkemCerpano ?? 0),
                        PocetCerpani = PocetCerpani + (other?.PocetCerpani ?? 0),
                        PocetDotaci = PocetDotaci + (other?.PocetDotaci ?? 0)
                    };
                }

                public override int NewSeasonStartMonth()
                {
                    return 1;
                }

                public override int UsualFirstYear()
                {
                    return 2000;
                }
            }
        }
    }
}
