﻿using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HlidacStatu.Util
{
    public static partial class DataValidators
    {
        private static readonly ILogger logger = Log.ForContext("SourceMethod", "DataValidators");

        static string root = null;
        static List<string> czobce = new List<string>();
        static List<string> skobce = new List<string>();
        static Dictionary<string, string> ciziStaty = new Dictionary<string, string>();

        static Devmasters.Cache.AWS_S3.Cache<string> czobceCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
    TimeSpan.Zero, "czobce.txt", (obj) =>
    {
        return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/czobce.txt").Result;
    }, null);

        static Devmasters.Cache.AWS_S3.Cache<string> skobceCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
    TimeSpan.Zero, "skobce.txt", (obj) =>
    {
        return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/skobce.txt").Result;
    }, null);

        static Devmasters.Cache.AWS_S3.Cache<string> statyCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
TimeSpan.Zero, "staty.txt", (obj) =>
{
return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/staty.txt").Result;
}, null);

        static DataValidators()
        {
            try
            {
                if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("WebAppDataPath")))
                {
                    root = Devmasters.Config.GetWebConfigValue("WebAppDataPath");
                }
                //else if (System.Web.HttpContext.Current != null) //inside web app
                //{
                //    root = System.Web.HttpContext.Current.Server.MapPath("~/App_Data/");
                //}
                else
                    root = Devmasters.IO.IOTools.GetExecutingDirectoryName(true);

                if (!root.EndsWith(Path.DirectorySeparatorChar))
                    root = root + Path.DirectorySeparatorChar;


                var tmp = statyCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Split('\t'))
                    .Select(mm =>
                    {
                        if (mm.Length == 1)
                            return new KeyValuePair<string, string>(Devmasters.TextUtil.RemoveDiacritics(mm[0]).Trim().ToLower(), "xx");
                        else
                            return new KeyValuePair<string, string>(Devmasters.TextUtil.RemoveDiacritics(mm[0]).Trim().ToLower(), mm[1].Length == 0 ? "xx" : mm[1].Trim());
                    });
                foreach (var kv in tmp.Where(m => !string.IsNullOrEmpty(m.Key)))
                {
                    if (!ciziStaty.ContainsKey(kv.Key))
                        ciziStaty.Add(kv.Key, kv.Value);
                }

                czobce = czobceCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => Devmasters.TextUtil.RemoveDiacritics(m.Trim()).ToLower())
                        .ToList();
                skobce = skobceCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => Devmasters.TextUtil.RemoveDiacritics(m.Trim()).ToLower())
                        .ToList();


            }
            catch (Exception e)
            {
                logger.Fatal(e, "Cannot initialize DataValidators class");
            }

        }

        /// <summary>
        /// Returns true if <paramref name="code"/> is a valid ISO 639 language code.
        /// </summary>
        /// <param name="code">Candidate code (“en”, “fr”, “spa”, …)</param>
        /// <param name="alpha">
        /// 2  → check against ISO 639-1 (two-letter codes)  
        /// 3  → check against ISO 639-2/3 (three-letter codes)
        /// </param>
        public static bool IsIso639LanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            code = code.Trim().ToLowerInvariant();

            // Neutral cultures = language-only, no region info (e.g. "en", not "en-US")
            var cultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);
            int alpha = code.Length;
            switch (alpha)
            {
                case 2:
                    return cultures
                           .Select(c => c.TwoLetterISOLanguageName.ToLowerInvariant())
                           .Distinct()
                           .Contains(code);

                case 3:
                    // Some cultures have 3-letter fallback “ivl” etc.; filter to length 3
                    return cultures
                           .Select(c => c.ThreeLetterISOLanguageName.ToLowerInvariant())
                           .Where(s => s.Length == 3)
                           .Distinct()
                           .Contains(code);

                default:
                    //throw new ArgumentOutOfRangeException(nameof(alpha), alpha, "alpha must be 2 or 3");
                    return false;
            }
        }

        public static string FirmaIcoZahranicni(string ico)
        {
            if (string.IsNullOrEmpty(ico))
                return null;

            var pref = Devmasters.RegexUtil.GetRegexGroupValue(ico, @"^(?<pref>\w{2}-).{1,}", "pref");

            return pref;//je-li prefix, je to zahranicni ico
        }
        public static bool IsFirmaIcoZahranicni(string ico)
        {
            return !string.IsNullOrEmpty(FirmaIcoZahranicni(ico));//je-li prefix, je to zahranicni ico
        }

        //zdroj http://devon.blog.zive.cz/2009/09/kontrola-rodneho-cisla/
        public static bool IsRcValid(string rc)
        {
            DateTime? ret;
            return IsRcValid(rc, out ret);
        }
        public static bool IsRcValid(string rc, out DateTime? birthDate)
        {
            birthDate = null;

            if (rc == null || (rc.Length < 9 || rc.Length > 10)) return false;
            Regex validCharsLenTest = new Regex(@"\d{9}[0-9aA]?");
            if (validCharsLenTest.IsMatch(rc) == false) return false;
            // RC.Lenght == 9 -> roky 1900 .. 1953
            // RC.Lenght == 10 -> roky 1954 .. 2053
            int year = Convert.ToInt32(rc.Substring(0, 2));
            if (rc.Length == 9 && year >= 54) return false;  // od 1.1.1954 ma RC 10 znaku
            if (rc.Length == 9 || (rc.Length == 10 && year >= 54))
            {
                year += 1900;
            }
            else
            {
                year += 2000;
            }
            // muzi: 1 .. 12, 21 .. 32
            // zeny: 51 .. 62, 71 .. 82
            int month = Convert.ToInt32(rc.Substring(2, 2));
            if (month > 70 && year > 2003) month -= 70;   // 53/2004 Sb.
            if (month > 50) month -= 50;
            if (month > 20 && year > 2003) month -= 20;   // 53/2004 Sb.
            if (month < 1 || month > 12) return false;
            int day = Convert.ToInt32(rc.Substring(4, 2));
            if (day > 50) day -= 50;                      //den + 50 = cizinec, zijici v CR
            if (day < 1 || day > 31) return false;

            // maji jen 30 dni
            if ((month == 4 || month == 6 || month == 9 || month == 11) && (day > 30)) return false;

            // prestupny unor 29 dni
            if ((month == 2) && (Devmasters.DT.Util.IsPrestupnyRok(year) == true) && (day > 29)) return false;

            // neprestupny unor 28 dni
            if ((month == 2) && (Devmasters.DT.Util.IsPrestupnyRok(year) == false) && (day > 28)) return false;

            birthDate = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local);
            if (birthDate > DateTime.Now)
            {
                birthDate = null;
                return false;
            }
            int rcBase = Convert.ToInt32(rc.Substring(0, 9));   // rrmmddccc
            if (rc.Length > 9)
            {
                string controlstring = rc.Substring(9, 1);
                int controlnum = 0;
                if (controlstring == "a" || controlstring == "A")
                {

                    controlnum = 0;
                }
                else
                {
                    controlnum = Convert.ToInt32(controlstring);
                }
                int mod = rcBase % 11;
                if (mod != controlnum)
                {
                    // zkusíme to jeste jednou pro 10
                    if (controlnum == 0 && mod == 10) return true;
                    // kdepak, je to spatne...
                    return false;
                }
            }
            return true;
        }


        public static bool CheckCZICO(string ico)
        {
            if (string.IsNullOrEmpty(ico))
                return false;

            if (!Devmasters.TextUtil.IsNumeric(ico))
                return false;
            if (ico.Length < 8)
            {
                ico = ico.PadLeft(8, '0');
            }

            if (ico.Length != 8)
                return false;

            int sum = 0;
            try
            {
                for (int i = 0; i < 7; i++)
                {
                    int num = int.Parse(ico[i].ToString());
                    sum = sum + num * (8 - i);
                }
                int control = ((11 - sum % 11) % 10);

                return (control == int.Parse(ico[7].ToString()));


            }
            catch (Exception)
            {
                return false;
            }

        }




        private static string CeskaAdresaObec(string adresa)
        {
            if (!string.IsNullOrEmpty(adresa))
            {
                string dadresa = Devmasters.TextUtil.RemoveDiacritics(adresa).ToLower().Trim();
                foreach (var o in czobce)
                {
                    //(\s|,|;)mexiko($|\s)
                    string reg = "(^|\\s|,|;|\\.)" + o.Replace(" ", "\\s{1,3}") + "(,|;|\\.|$|\\s)";

                    if (Regex.IsMatch(dadresa, reg, Consts.DefaultRegexQueryOption))
                        return o;
                }
            }

            return null;
        }

        private static string SKAdresaObec(string adresa)
        {
            if (!string.IsNullOrEmpty(adresa))
            {
                string dadresa = Devmasters.TextUtil.RemoveDiacritics(adresa).ToLower().Trim();
                foreach (var o in skobce)
                {
                    //(\s|,|;)mexiko($|\s)
                    string reg = "(^|\\s|,|;|\\.)" + o.Replace(" ", "\\s{1,3}") + "(,|;|\\.|$|\\s)";

                    if (Regex.IsMatch(dadresa, reg, Consts.DefaultRegexQueryOption))
                        return o;

                    //if (dadresa.Contains(o))
                    //    return o;
                }
            }

            return null;
        }

    }
}
