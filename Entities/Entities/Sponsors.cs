using System;

namespace HlidacStatu.Entities
{
    public class Sponsors
    {




        public class Sponzorstvi<T>
            where T : class, IBookmarkable //T Osoba nebo Firma
        {
            public T Sponzor { get; set; }
            public String Strana { get; set; }
            public decimal CastkaCelkem { get; set; }
            public int? Rok { get; set; }

            public static explicit operator
                Sponzorstvi<IBookmarkable>(Sponzorstvi<T> d) // implicit digit to byte conversion operator
            {
                return new Sponzorstvi<IBookmarkable>()
                {
                    Strana = d.Strana,
                    CastkaCelkem = d.CastkaCelkem,
                    Rok = d.Rok,
                    Sponzor = (IBookmarkable)d.Sponzor
                };
            }
        }

        public static string GetStranaUrl(string strana, bool local = true)
        {
            string url = $"/Sponzori/strana?id={System.Net.WebUtility.UrlEncode(strana)}";
            if (local == false)
                return "http://www.hlidacstatu.cz" + url;
            else
                return url;
        }

        public static string GetStranaHtmlLink(string strana, bool local = true)
        {
            return $"<a href='{GetStranaUrl(strana, local)}'>{strana}</a>";
        }

        public enum SponzoringDataType
        {
            All = 0,
            Osoby = 1,
            Firmy = 1
        }

        public static string GetStranaSponzoringUrl(string strana, int rok, SponzoringDataType typ, bool local = true)
        {
            string url = $"/Sponzori/seznam?id={System.Net.WebUtility.UrlEncode(strana)}&rok={rok}&typ={(int)typ}";
            if (local == false)
                return "http://www.hlidacstatu.cz" + url;
            else
                return url;
        }

        public static string GetStranaSponzoringHtmlLink(string strana, int rok, SponzoringDataType typ,
            bool local = true)
        {
            return $"<a href='{GetStranaSponzoringUrl(strana, rok, typ, local)}'>Dary osob pro {strana} v {rok}</a>";
        }
    }
}