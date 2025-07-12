using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.KIndex
{
    public class Consts
    {
        public static string[] KIndexExceptions = new string[] { };

        public static int[] XAvailableCalculationYears = null;
        public static int[] ToCalculationYears = null;

        //interval pod limit
        public static decimal IntervalOkolo = 0.11m;


        //ceny bez DPH
        public static Dictionary<HintSmlouva.ULimituTyp, decimal> LimityDo2025 = new()
            {
                { HintSmlouva.ULimituTyp.LimitMalehoRozsahuDodavkySluzby, 2_000_000m }, //do 3.4.2025
                { HintSmlouva.ULimituTyp.LimitMalehoRozsahuStavebniPrace, 6_000_000m }, //do 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniDodavkySluzbyUstredniOrgany, 3_494_000m }, //do 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniDodavkySluzby, 5_401_000m }, //do 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniStavebniPrace, 135_348_000 }
            };
        public static DateTime ZmenaLimituZZVZ_2025 = new DateTime(2025, 4, 3);
        public static Dictionary<HintSmlouva.ULimituTyp, decimal> LimityOd2025 = new()
            {
                { HintSmlouva.ULimituTyp.LimitMalehoRozsahuDodavkySluzby, 3_000_000m }, //od 3.4.2025
                { HintSmlouva.ULimituTyp.LimitMalehoRozsahuStavebniPrace, 9_000_000m }, //od 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniDodavkySluzbyUstredniOrgany, 3_494_000m }, //od 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniDodavkySluzby, 5_401_000m }, //od 3.4.2025
                { HintSmlouva.ULimituTyp.LimitPodlimitniStavebniPrace, 135_348_000 }
            };


        public const int MinPocetSmluvPerYearIfHasSummarySmluv = 30;
        public const int MinPocetSmluvPerYear = 60;
        public const int MinSmluvySummaryPerYear = 48_000_000;
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
