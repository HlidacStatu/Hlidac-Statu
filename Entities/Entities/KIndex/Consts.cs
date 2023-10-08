using System;
using System.Linq;

namespace HlidacStatu.Entities.KIndex
{
    public class Consts
    {
        public static string[] KIndexExceptions = new string[] { };

        public static int[] XAvailableCalculationYears = null;
        public static int[] ToCalculationYears = null;

        public const decimal IntervalOkolo = 0.11m;

        public const decimal Limit1bezDPH_To = 2000000;
        public const decimal Limit1bezDPH_From = Limit1bezDPH_To - (Limit1bezDPH_To * IntervalOkolo);
        public const decimal Limit2bezDPH_To = 6000000;
        public const decimal Limit2bezDPH_From = Limit2bezDPH_To - (Limit2bezDPH_To * IntervalOkolo);


        public const int MinSmluvPerYear = 60;
        public const int MinSumSmluvPerYear = 48000000;
        public const decimal MinSmluvPerYearKIndexValue = -10000m;

        public const decimal BonusPod50K_1 = 0.25m;
        public const decimal BonusPod50K_2 = 0.5m;
        public const decimal BonusPod50K_3 = 0.75m;


        static Consts()
        {

            ToCalculationYears = Enumerable
                .Range(2017, DateTime.Now.Year - 2017 - (DateTime.Now.Month >= 4 ? 0 : 1))
                .OrderBy(o=>o)
                .ToArray();
        }


        
    }
}
