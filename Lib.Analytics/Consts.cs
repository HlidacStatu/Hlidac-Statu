using System;
using System.Linq;

namespace HlidacStatu.Lib.Analytics
{
    public static class Consts
    {
        public static int[] AllYears = Enumerable.Range(2000, 100).ToArray();
        public static int[] RegistrSmluvYearsList = Enumerable.Range(2016, DateTime.Now.Year - 2016 + 1).ToArray();
        public static int[] VZYearsList = Enumerable.Range(2010, DateTime.Now.Year - 2010 + 1).ToArray();

    }
}
