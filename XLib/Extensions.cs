using Devmasters.Lang.CS;

using HlidacStatu.Entities;


namespace HlidacStatu.XLib
{
    public static class Extensions
    {
        public static string ToNiceLinkString(this Smlouva.Statistics.Data stat, IBookmarkable item,
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
        public static string ToNiceLinkString_1pad(this Firma.Statistics.Dotace stat, IBookmarkable item,
    bool html = true, string customUrl = null, bool twoLines = false)
        {
            if (html)
            {
                var s = "<a href='" + (customUrl ?? (item?.GetUrl(true) ?? "")) + "'>" +
                        Plural.Get(stat.PocetDotaci, "{0} dotace;{0} dotace;{0} dotací") +
                        "</a>" + (twoLines ? "<br />" : " ") +
                        "ve výši " +
                        Smlouva.NicePrice(stat.CelkemPrideleno, html: true, shortFormat: true);
                return s;
            }
            else
                return Plural.Get(stat.PocetDotaci, "{0} dotace;{0} dotace;{0} dotací") +
                       " ve výši " + Smlouva.NicePrice(stat.CelkemPrideleno, html: false, shortFormat: true);
        }
        public static string ToNiceLinkString_4pad(this Firma.Statistics.Dotace stat, IBookmarkable item,
bool html = true, string customUrl = null, bool twoLines = false)
        {
            if (html)
            {
                var s = "<a href='" + (customUrl ?? (item?.GetUrl(true) ?? "")) + "'>" +
                        Plural.Get(stat.PocetDotaci, "{0} dotaci;{0} dotace;{0} dotací") +
                        "</a>" + (twoLines ? "<br />" : " ") +
                        "ve výši " +
                        Smlouva.NicePrice(stat.CelkemPrideleno, html: true, shortFormat: true);
                return s;
            }
            else
                return Plural.Get(stat.PocetDotaci, "{0} dotaci;{0} dotace;{0} dotací") +
                       " ve výši " + Smlouva.NicePrice(stat.CelkemPrideleno, html: false, shortFormat: true);
        }
    }
}