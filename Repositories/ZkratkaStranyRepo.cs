using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

using HlidacStatu.Entities;
using HlidacStatu.Util;

namespace HlidacStatu.Repositories
{
    public static class ZkratkaStranyRepo
    {

        /// <returns> Dictionary; Key=Ico, Value=Zkratka </returns>
        public static Dictionary<string, string> ZkratkyVsechStran()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.ZkratkaStrany.ToDictionary(z => z.Ico, e => e.KratkyNazev);
            }
        }

        public static string IcoStrany(string zkratka)
        {
            if (DataValidators.CheckCZICO(Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka)))
            {
                var f = Firmy.Get(Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka));
                if (f.Valid && f.Kod_PF == 711)
                    return Devmasters.TextUtil.NormalizeToNumbersOnly(zkratka);
                else
                    return zkratka; //TODO co delat, kdyz ICO neni politicka strana
            }
            using (DbEntities db = new DbEntities())
            {
                return db.ZkratkaStrany
                    .AsNoTracking()
                    .Where(zs => zs.KratkyNazev.ToLower() == zkratka.Trim().ToLower())
                    .Select(zs => zs.Ico)
                    .FirstOrDefault();
            }
        }

        public static string[,] NazvyStranZkratky = {
            {"strana zelených","SZ"},
            {"česká pirátská strana","Piráti" },
            {"pirátská strana","Piráti" },
            {"strana práv občanů","SPO" },
            {"svoboda a přímá demokracie","SPD" },
            {"rozumní - stop migraci a diktátu eu","Rozumní" },
            {"věci veřejné","VV" },
            {"starostové a nezávislí","STAN" },
        };

        public static string StranaZkratka(string strana, int maxlength = 20)
        {
            if (string.IsNullOrEmpty(strana))
                return string.Empty;

            var lstrana = strana.ToLower();
            //je na seznamu?
            for (int i = 0; i < NazvyStranZkratky.GetLength(0); i++)
            {
                if (NazvyStranZkratky[i, 0] == lstrana)
                    return NazvyStranZkratky[i, 1];
            }

            var words = Devmasters.TextUtil.GetWords(strana);
            if (Devmasters.TextUtil.CountWords(strana) > 3)
            {
                //vratim zkratku z prvnich pismen
                var res = "";
                foreach (var w in words)
                {
                    char ch = w.First();
                    if (
                        System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.UppercaseLetter
                        ||
                        System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.TitlecaseLetter
                    )
                        res = res + ch;
                }
                if (res.Length > 2)
                    return res;
            }
            return Devmasters.TextUtil.ShortenText(strana, maxlength);
        }
    }
}