using Devmasters.Lang.CS;

using HlidacStatu.Entities;


namespace HlidacStatu.XLib
{
    public static class Extensions
    {
        public static string ToNiceString(this Smlouva.Statistics.Data stat, IBookmarkable item,
            bool html = true, string customUrl = null, bool twoLines = false)
        {
            if (html)
            {
                var s = "<a href='" + (customUrl ?? (item?.GetUrl(true) ?? "")) + "'>" +
                        Plural.Get(stat.PocetSmluv, "{0} smlouva;{0} smlouvy;{0} smluv") +
                        "</a>" + (twoLines ? "<br />" : " za ") +
                        "celkem " +
                        Smlouva.NicePrice(stat.CelkovaHodnotaSmluv, html: true, shortFormat: true);
                return s;
            }
            else
                return Plural.Get(stat.PocetSmluv, "{0} smlouva;{0} smlouvy;{0} smluv") +
                       " za celkem " + Smlouva.NicePrice(stat.CelkovaHodnotaSmluv, html: false, shortFormat: true);
        }
    }
}