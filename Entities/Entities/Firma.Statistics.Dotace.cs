using System.Collections.Generic;
using HlidacStatu.Lib.Analytics;

namespace HlidacStatu.Entities
{
    public partial class Firma
    {
        public partial class Statistics
        {

            public class Subsidy : CoreStat, IAddable<Subsidy>
            {
                public int PocetDotaci { get; set; }
                public decimal CelkemPrideleno { get; set; }
                
                /// <summary>
                /// Vyplněno pouze pro dotace holdingu
                /// </summary>
                public Dictionary<string, decimal> JednotliveFirmy { get; set; }


                public Subsidy Add(Subsidy other)
                {
                    return new Subsidy()
                    {
                        CelkemPrideleno = CelkemPrideleno + (other?.CelkemPrideleno ?? 0),
                        PocetDotaci = PocetDotaci + (other?.PocetDotaci ?? 0)
                    };
                }
                public Subsidy Subtract(Subsidy other)
                {
                    return new Subsidy()
                    {
                        CelkemPrideleno = CelkemPrideleno - (other?.CelkemPrideleno ?? 0),
                        PocetDotaci = PocetDotaci - (other?.PocetDotaci ?? 0)
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
