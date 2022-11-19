using System.Collections.Generic;

namespace HlidacStatu.Lib.Analytics
{
    public static class Extensions
    {
        public static StatisticsPerYear<T> AggregateStats<T>(this IEnumerable<StatisticsPerYear<T>> statistics, int[] onlyYears = null)
            where T : CoreStat, IAddable<T>, new()
        {
            return StatisticsPerYear<T>.AggregateStats(statistics, onlyYears);
        }
        public static StatisticsPerYear<T> SubtractFromStats<T>(this IEnumerable<StatisticsPerYear<T>> statistics, int[] onlyYears = null)
            where T : CoreStat, IAddable<T>, new()
        {
            return StatisticsPerYear<T>.SubtractFromStats(statistics, onlyYears);
        }

        public static string Formatted(this SimpleStat item, bool html = true, string url = null, bool twoLines = false, string textIfZero = null)
        {
            if (item.Pocet == 0 && textIfZero != null)
            {
                return textIfZero;
            }
            if (html && !string.IsNullOrEmpty(url))
            {
                var s = "<a href='" + url + "'>" +
                            Devmasters.Lang.CS.Plural.Get(item.Pocet, "{0} smlouva;{0} smlouvy;{0} smluv") +
                        "</a>" + (twoLines ? "<br />" : " za ") +
                        "celkem " +
                        HlidacStatu.Util.RenderData.NicePrice(item.CelkemCena, html: true, shortFormat: true);
                return s;
            }
            else
                return Devmasters.Lang.CS.Plural.Get(item.Pocet, "{0} smlouva;{0} smlouvy;{0} smluv") +
                    " za celkem " + HlidacStatu.Util.RenderData.NicePrice(item.CelkemCena, html: false, shortFormat: true);

        }


    }
}
