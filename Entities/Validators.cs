using Devmasters;

using EnumsNET;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Serilog;
using Amazon.Runtime.Internal.Util;
using Microsoft.EntityFrameworkCore.Query;

namespace HlidacStatu.Entities
{

    public static class Validators
    {
        public static HashSet<string> Jmena = new HashSet<string>();
        public static HashSet<string> Prijmeni = new HashSet<string>();
        public static HashSet<string> TopJmena = new HashSet<string>();
        public static HashSet<string> TopPrijmeni = new HashSet<string>();

        public static Dictionary<string, string> CPVKody = new Dictionary<string, string>();

        static RegexOptions regexOptions = Util.Consts.DefaultRegexQueryOption;

        //L1 - 10 let
        //L2 - 10 let
        static Devmasters.Cache.AWS_S3.Cache<string> jmenaCache = new Devmasters.Cache.AWS_S3.Cache<string>(
            new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
            Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
            Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "jmena.txt", (obj) =>
                {
                    return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/jmena.txt").Result;

                }, null);

        //L1 - 10 let
        //L2 - 10 let
        static Devmasters.Cache.AWS_S3.Cache<string> prijmeniCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "prijmeni.txt", (obj) =>
            {
                return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/prijmeni.txt").Result;
            }, null);

        //L1 - 10 let
        //L2 - 10 let
        static Devmasters.Cache.AWS_S3.Cache<string> topjmenaCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "topjmena.txt", (obj) =>
            {
                return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/topjmena.txt").Result;
            }, null);

        //L1 - 10 let
        //L2 - 10 let
        static Devmasters.Cache.AWS_S3.Cache<string> topprijmeniCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "topprijmeni.txt", (obj) =>
            {
                return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/topprijmeni.txt").Result;
            }, null);

        //L1 - 10 let
        //L2 - 10 let
        static Devmasters.Cache.AWS_S3.Cache<string> cpvCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "CPV_CS.txt", (obj) =>
            {
                return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/CPV_CS.txt").Result;
            }, null);

        static Validators()
        {
            Init();
        }
        static bool initialized = false;

        static void Init()
        {
            if (initialized) 
                return;

            Devmasters.DT.StopWatchLaps swl = new Devmasters.DT.StopWatchLaps();

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod())+" var jmeno");

            Jmena = new HashSet<string>(jmenaCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(m => TextUtil.RemoveDiacritics(m.ToLower().Trim()))
                .Distinct());

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) + " var prijmeni");
            Prijmeni = new HashSet<string>(prijmeniCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(m => TextUtil.RemoveDiacritics(m.ToLower().Trim()))
                .Distinct());

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) + " var topjmena");
            TopJmena = new HashSet<string>(topjmenaCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(m => TextUtil.RemoveDiacritics(m.ToLower().Trim()))
                .Distinct());
            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) + " var toprijmeni");
            TopPrijmeni = new HashSet<string>(topprijmeniCache.Get().Split("\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(m => TextUtil.RemoveDiacritics(m.ToLower().Trim()))
                .Distinct());

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) + " loading cpv_cs");
            Log.ForContext(typeof(Validators)).Information("Static data - loading cpv_cs");

            using (StringReader r = new StringReader(cpvCache.Get()))
            {
                var csv = new CsvHelper.CsvReader(r,
                    new CsvHelper.Configuration.CsvConfiguration(Util.Consts.csCulture)
                    { HasHeaderRecord = true, Delimiter = "\t" });
                csv.Read();
                csv.ReadHeader();
                csv.Read(); //skip second line
                while (csv.Read())
                {
                    string kod = csv.GetField<string>("Kód")?.Trim();
                    string text = csv.GetField<string>("Název")?.Trim();
                    if (!string.IsNullOrEmpty(kod) && !string.IsNullOrEmpty("text"))
                        CPVKody.Add(kod, text);
                }
            }
            swl.StopAll();
            Log.ForContext(typeof(Validators)).Warning("Static data times:" + swl.FormatSummary());
            initialized = true;
        }

        public static bool IsOsoba(string text)
        {
            return JmenoInText(text) != null;
        }

        public static (string titulyPred, string titulyPo, string jmeno) SeparateNameFromTitles(string text)
        {
            if (text == null)
                return (null, null, null);
            if (text == "")
                return ("", "", "");
            text = text.ToLower();

            var (titlesBefore, separatedText1) =
                SeparateTitles(text, Osoba.TitulyPred.OrderByDescending(t => t.Length).ToArray());
            var (titlesAfter, separatedText2) =
                SeparateTitles(separatedText1, Osoba.TitulyPo
                    .Where(t => t != "HONS")
                    .OrderByDescending(t => t.Length).ToArray());

            //text = Regex.Replace(separatedText2, @"(^\w)|(\s\w)", m => m.Value.ToUpper());
            text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(separatedText2);

            text = Regex.Replace(text, @"\s{2,}", " ");

            return (string.Join(" ", titlesBefore), string.Join(" ", titlesAfter), text.Trim(" ,.;!?:-".ToCharArray()));
        }

        private static (string[] titles, string text) SeparateTitles(string text, string[] titles)
        {
            var titlesFound = new List<string>();

            //remove whole titles
            foreach (var title in titles)
            {
                string lowerTitle = title.ToLower();
                if (lowerTitle.Contains("."))
                    lowerTitle = lowerTitle.Replace(".", "\\.?");

                if (Regex.IsMatch(text, $"\\b{lowerTitle}\\b"))
                {
                    titlesFound.Add(title);
                    text = Regex.Replace(text, lowerTitle, " ");
                }
            }

            //remove titles which are connected to a name (can do only with titles containing ".")
            foreach (var title in titles)
            {
                if (!title.Contains("."))
                    continue;

                string lowerTitle = title.ToLower();

                if (text.Contains(lowerTitle))
                {
                    titlesFound.Add(title);
                    text = text.Replace(lowerTitle, " ");
                }
            }

            return (titlesFound.ToArray(), text);
        }
        public static string[] JmenaPrijmeniInText(string text)
        {
            string normalizedText = Regex.Replace(text, @"[^a-zA-Z\p{L}\p{M}.,]", " ");
            normalizedText = TextUtil.ReplaceDuplicates(normalizedText, ' ').Trim();
            //remove tituly
            normalizedText = SeparateNameFromTitles(normalizedText).jmeno;

            var words = normalizedText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> ret = new List<string>();
            foreach (var w in words)
            {
                var normalizedW = TextUtil.RemoveDiacritics(w).ToLower();
                
                if (!string.IsNullOrEmpty(normalizedW))
                {
                    CompareResult w0 = CompareName(normalizedW);
                    if (w0.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInTopPrijmeni))
                        ret.Add(w);
                    else if (w0.HasAnyFlags(CompareResult.FoundInPrijmeni | CompareResult.FoundInJmeno))
                        ret.Add(w);

                }
            }
            return ret.ToArray();

        }
        public static Osoba JmenoInText(string text, bool preferAccurateResult = false)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string normalizedText = Regex.Replace(text, @"[^a-zA-Z\p{L}\p{M}.,]", " ");
            normalizedText = TextUtil.ReplaceDuplicates(normalizedText, ' ').Trim();


            // Zakomentováno 12.12.2019 - při nahrávání dotací byl problém se jménem Ĺudovit
            //fix errors Ĺ => Í,ĺ => í 
            //normalizedText = normalizedText.Replace((char)314, (char)237).Replace((char)313, (char)205);

            //remove tituly
            var titulyPo = Osoba.TitulyPo.Select(m => TextUtil.RemoveDiacritics(m).ToLower()).ToArray();
            var titulyPred = Osoba.TitulyPred.Select(m => TextUtil.RemoveDiacritics(m).ToLower()).ToArray();

            List<string> lWords = new List<string>();

            var titulPred = string.Empty;
            var titulPo = string.Empty;

            var normalizedWords = normalizedText
                .Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w=> !string.IsNullOrEmpty(w))
                .Where(w=> titulyPo.Contains(w.RemoveDiacritics().ToLower()) == false)
                .Where(w => titulyPred.Contains(w.RemoveDiacritics().ToLower()) == false)
                .ToArray()
                ;
            foreach (var w in normalizedWords)
            {
                var newW = w;
                string cw = TextUtil.RemoveDiacritics(w).ToLower();
                for (int i = 0; i < titulyPo.Length; i++)
                {
                    var t = titulyPo[i];
                    if (cw == t)
                    {
                        newW = "";
                        titulPo = titulPo + " " + Osoba.TitulyPo[i];
                        break;
                    }
                    else if (t.EndsWith(".") && cw == t.Substring(0, t.Length - 1))
                    {
                        newW = "";
                        titulPo = titulPo + " " + Osoba.TitulyPo[i];
                        break;
                    }
                }

                for (int i = 0; i < titulyPred.Length; i++)
                {
                    var t = titulyPred[i];

                    if (cw == t)
                    {
                        newW = "";
                        titulPred = titulPred + " " + Osoba.TitulyPred[i];
                        break;
                    }
                    else if (t.EndsWith(".") && cw == t.Substring(0, t.Length - 1))
                    {
                        newW = "";
                        titulPred = titulPred + " " + Osoba.TitulyPred[i];
                        break;
                    }
                }

                titulPo = titulPo.Trim();
                titulPred = titulPred.Trim();
                lWords.Add(newW);
            }

            //reset normalizedText after titulPred, titulPo
            normalizedText = String.Join(" ", lWords).Trim();
            normalizedText = TextUtil
                .ReplaceDuplicates(normalizedText.Replace(".", ". "), ' ');
            normalizedText = TextUtil
                .ReplaceDuplicates(normalizedText, " ");
            string[] wordsFromNormalizedText =
                normalizedText.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string[] words = wordsFromNormalizedText.Select(m => TextUtil.RemoveDiacritics(m).ToLower()).ToArray();


            int maxWords = 2;
            int minWords = 2;
            for (int firstWord = 0; firstWord < words.Length - 1; firstWord++)
            {
                for (int takeWords = Math.Min(maxWords, words.Count() - firstWord); takeWords >= minWords; takeWords--)
                {
                    var currWords = words.Skip(firstWord).Take(takeWords).ToArray();
                    var origWords = wordsFromNormalizedText.Skip(firstWord).Take(takeWords).ToArray();


                    CompareResult w0 = CompareName(currWords[0]);
                    CompareResult w1 = CompareName(currWords[1]);
                    if (w0 == w1 && w0 != CompareResult.NotFound &&
                        currWords[0] == currWords[1]) //stejne jmeno a prijmeni MUDr. Tomáš Tomáš, PH.D.
                        return new Osoba()
                        { Jmeno = origWords[0], Prijmeni = origWords[1], TitulPred = titulPred, TitulPo = titulPo };

                    if (
                        w0.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInJmeno)
                        && !w0.HasAnyFlags(CompareResult.FoundInTopPrijmeni)
                        && w0 != CompareResult.NotFound
                        &&
                        w1.HasAnyFlags(CompareResult.FoundInTopPrijmeni | CompareResult.FoundInPrijmeni)
                        && !w1.HasAnyFlags(CompareResult.FoundInTopJmeno)
                        && w1 != CompareResult.NotFound
                    )
                        return new Osoba()
                        { Jmeno = origWords[0], Prijmeni = origWords[1], TitulPred = titulPred, TitulPo = titulPo };
                    else if (
                        w1.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInJmeno)
                        && !w1.HasAnyFlags(CompareResult.FoundInTopPrijmeni)
                        && w1 != CompareResult.NotFound
                        &&
                        w0.HasAnyFlags(CompareResult.FoundInTopPrijmeni | CompareResult.FoundInPrijmeni)
                        && !w0.HasAnyFlags(CompareResult.FoundInTopJmeno)
                        && w0 != CompareResult.NotFound
                    )
                        return new Osoba()
                        { Jmeno = origWords[1], Prijmeni = origWords[0], TitulPred = titulPred, TitulPo = titulPo };

                    //situace ala
                    //w0 FoundInTopPrijmeni | FoundInTopJmeno    
                    //w1  FoundInPrijmeni 
                    if (
                        w0.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInTopPrijmeni)
                        && !w1.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInJmeno)
                        && w1 != CompareResult.NotFound
                    )
                        return new Osoba()
                        { Jmeno = origWords[0], Prijmeni = origWords[1], TitulPred = titulPred, TitulPo = titulPo };
                    if (
                        w1.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInTopPrijmeni)
                        && !w0.HasAnyFlags(CompareResult.FoundInTopJmeno | CompareResult.FoundInJmeno)
                        && w0 != CompareResult.NotFound
                    )
                        return new Osoba()
                        { Jmeno = origWords[1], Prijmeni = origWords[0], TitulPred = titulPred, TitulPo = titulPo };

                    //spocitej vahy
                    int w0_prijmeni_weight = 0;
                    if (w0.HasAnyFlags(CompareResult.FoundInTopPrijmeni))
                        w0_prijmeni_weight = 2;
                    else if (w0.HasAnyFlags(CompareResult.FoundInPrijmeni))
                        w0_prijmeni_weight = 1;
                    int w0_jmeno_weight = 0;
                    if (w0.HasAnyFlags(CompareResult.FoundInTopJmeno))
                        w0_jmeno_weight = 2;
                    else if (w0.HasAnyFlags(CompareResult.FoundInJmeno))
                        w0_jmeno_weight = 1;

                    int w1_prijmeni_weight = 0;
                    if (w1.HasAnyFlags(CompareResult.FoundInTopPrijmeni))
                        w1_prijmeni_weight = 2;
                    else if (w1.HasAnyFlags(CompareResult.FoundInPrijmeni))
                        w1_prijmeni_weight = 1;
                    int w1_jmeno_weight = 0;
                    if (w1.HasAnyFlags(CompareResult.FoundInTopJmeno))
                        w1_jmeno_weight = 2;
                    else if (w1.HasAnyFlags(CompareResult.FoundInJmeno))
                        w1_jmeno_weight = 1;

                    if (w0_jmeno_weight>w0_prijmeni_weight && w1_prijmeni_weight>=w1_jmeno_weight)
                        return new Osoba()
                        { Jmeno = origWords[0], Prijmeni = origWords[1], TitulPred = titulPred, TitulPo = titulPo };
                    if (w0_jmeno_weight >= w0_prijmeni_weight && w1_prijmeni_weight > w1_jmeno_weight)
                        return new Osoba()
                        { Jmeno = origWords[0], Prijmeni = origWords[1], TitulPred = titulPred, TitulPo = titulPo };

                    if (w0_jmeno_weight < w0_prijmeni_weight && w1_prijmeni_weight <= w1_jmeno_weight)
                        return new Osoba()
                        { Jmeno = origWords[1], Prijmeni = origWords[0], TitulPred = titulPred, TitulPo = titulPo };
                    if (w0_jmeno_weight >= w0_prijmeni_weight && w1_prijmeni_weight > w1_jmeno_weight)
                        return new Osoba()
                        { Jmeno = origWords[1], Prijmeni = origWords[0], TitulPred = titulPred, TitulPo = titulPo };


                    if (preferAccurateResult)
                        return null;

                    Osoba o = new Osoba();
                    o.TitulPred = titulPred;
                    o.TitulPo = titulPo;

                    if (w0.HasFlag(CompareResult.FoundInTopJmeno))
                        o.Jmeno = origWords[0];
                    if (string.IsNullOrEmpty(o.Jmeno)
                        && origWords.Length > 1
                        && w1.HasFlag(CompareResult.FoundInTopJmeno)
                    )
                        o.Jmeno = origWords[1];
                    if (w1.HasFlag(CompareResult.FoundInTopPrijmeni)
                        && origWords.Length > 1
                        && o.Jmeno != origWords[1]
                    )
                        o.Prijmeni = origWords[1];
                    if (string.IsNullOrEmpty(o.Prijmeni)
                        && w0.HasFlag(CompareResult.FoundInTopPrijmeni)
                        && o.Jmeno != origWords[0]
                    )
                        o.Prijmeni = origWords[0];

                    if (string.IsNullOrEmpty(o.Jmeno)
                        && w0.HasFlag(CompareResult.FoundInJmeno)
                        && o.Prijmeni != origWords[0]
                    )
                        o.Jmeno = origWords[0];
                    if (string.IsNullOrEmpty(o.Jmeno)
                        && w1.HasFlag(CompareResult.FoundInJmeno)
                        && origWords.Length > 1
                        && o.Prijmeni != origWords[1]
                    )
                        o.Jmeno = origWords[1];

                    if (string.IsNullOrEmpty(o.Prijmeni)
                        && w1.HasFlag(CompareResult.FoundInPrijmeni)
                        && origWords.Length > 1
                        && o.Jmeno != origWords[1]
                    )
                        o.Prijmeni = origWords[1];

                    if (string.IsNullOrEmpty(o.Prijmeni)
                        && w0.HasFlag(CompareResult.FoundInPrijmeni)
                        && o.Jmeno != origWords[0]
                    )
                        o.Prijmeni = origWords[0];
                    if (!string.IsNullOrEmpty(o.Jmeno) && !string.IsNullOrEmpty(o.Prijmeni))
                        return o;


                    //if (w1.HasFlag(CompareResult.FoundInTopJmeno))
                }
            }

            return null;
        }


        [Flags]
        private enum CompareResult
        {
            NotFound = 0,
            FoundInPrijmeni = 1,
            FoundInTopPrijmeni = 2,

            FoundInJmeno = 4,
            FoundInTopJmeno = 8,
        }

        private static CompareResult CompareName(string word)
        {
            var ret = CompareResult.NotFound;
            if (TopJmena.Contains(word))
                ret = ret | CompareResult.FoundInTopJmeno;
            if (TopPrijmeni.Contains(word))
                ret = ret | CompareResult.FoundInTopPrijmeni;
            if (Jmena.Contains(word) && !(ret.HasFlag(CompareResult.FoundInTopJmeno)))
                ret = ret | CompareResult.FoundInJmeno;
            if (Prijmeni.Contains(word) && !(ret.HasFlag(CompareResult.FoundInTopPrijmeni)))
                ret = ret | CompareResult.FoundInPrijmeni;

            return ret;
        }

        public static RegexOptions DefaultRegexOptions =
            ((RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline)
             | RegexOptions.IgnoreCase);


        public static string[] IcosInText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new string[] { };

            var numbers = RegexUtil.GetRegexGroupValues(text, @"(\s|\D|^)(?<ico>\d{8})(\s|\D|$)", "ico");
            List<string> icos = new List<string>();
            foreach (var num in numbers)
            {
                if (Util.DataValidators.CheckCZICO(num))
                    icos.Add(num);
            }

            return icos.ToArray();
        }

        public static Dictionary<int, DateTime> DatesInText(string value)
        {
            Dictionary<int, DateTime> ret = new Dictionary<int, DateTime>();
            if (string.IsNullOrEmpty(value))
                return null;
            var ms = dateInTextRegex.Matches(value);

            if (ms.Count > 0)
            {
                foreach (Match m in ms)
                {
                    try
                    {
                        int day = Convert.ToInt32(m.Groups["d"].Value);
                        int month = Convert.ToInt32(m.Groups["m"].Value);
                        int year = Convert.ToInt32(m.Groups["y"].Value);
                        if (m.Groups["y"].Value.Length == 2)
                            year = 1900 + year;

                        ret.Add(m.Index, new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Local));
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                return ret;
            }
            else
                return null;
        }

        static Regex dateInTextRegex = new Regex(
            @"(?<date>
				(
					(?<d>(\d{1,2}))
					\s?[.]
					(?<m>(1|2|3|4|5|6|7|8|9|01|02|03|04|05|06|07|08|09|10|11|12))
					\s?[.]
					(?<y>(\d{2}(\s|$)|(20\d{2}|19\d{2})(\s|$) ) )
					)
				)", DefaultRegexOptions);



        /// <summary>
        /// Compares the properties of two objects of the same type and returns if all properties are equal.
        /// </summary>
        /// <param name="objectA">The first object to compare.</param>
        /// <param name="objectB">The second object to compre.</param>
        /// <param name="ignoreList">A list of property names to ignore from the comparison.</param>
        /// <returns><c>true</c> if all property values are equal, otherwise <c>false</c>.</returns>
        public static bool AreObjectsEqual(object objectA, object objectB, bool debug = false,
            params string[] ignoreList)
        {
            bool result;

            if (objectA != null && objectB != null)
            {
                Type objectType;

                objectType = objectA.GetType();

                result = true; // assume by default they are equal

                foreach (PropertyInfo propertyInfo in objectType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.CanRead && !ignoreList.Contains(p.Name)))
                {
                    object valueA;
                    object valueB;

                    valueA = propertyInfo.GetValue(objectA, null);
                    valueB = propertyInfo.GetValue(objectB, null);

                    // if it is a primative type, value type or implements IComparable, just directly try and compare the value
                    if (CanDirectlyCompare(propertyInfo.PropertyType))
                    {
                        if (!AreValuesEqual(valueA, valueB))
                        {
                            if (debug)
                                Console.WriteLine("Mismatch with property '{0}.{1}' found.", objectType.FullName,
                                    propertyInfo.Name);
                            result = false;
                        }
                    }
                    // if it implements IEnumerable, then scan any items
                    else if (typeof(IEnumerable).IsAssignableFrom(propertyInfo.PropertyType))
                    {
                        IEnumerable<object> collectionItems1;
                        IEnumerable<object> collectionItems2;
                        int collectionItemsCount1;
                        int collectionItemsCount2;

                        // null check
                        if (valueA == null && valueB != null || valueA != null && valueB == null)
                        {
                            if (debug)
                                Console.WriteLine("Mismatch with property '{0}.{1}' found.", objectType.FullName,
                                    propertyInfo.Name);
                            result = false;
                        }
                        else if (valueA != null && valueB != null)
                        {
                            collectionItems1 = ((IEnumerable)valueA).Cast<object>();
                            collectionItems2 = ((IEnumerable)valueB).Cast<object>();
                            collectionItemsCount1 = collectionItems1.Count();
                            collectionItemsCount2 = collectionItems2.Count();

                            // check the counts to ensure they match
                            if (collectionItemsCount1 != collectionItemsCount2)
                            {
                                if (debug)
                                    Console.WriteLine("Collection counts for property '{0}.{1}' do not match.",
                                        objectType.FullName, propertyInfo.Name);
                                result = false;
                            }
                            // and if they do, compare each item... this assumes both collections have the same order
                            else
                            {
                                for (int i = 0; i < collectionItemsCount1; i++)
                                {
                                    object collectionItem1;
                                    object collectionItem2;
                                    Type collectionItemType;

                                    collectionItem1 = collectionItems1.ElementAt(i);
                                    collectionItem2 = collectionItems2.ElementAt(i);
                                    collectionItemType = collectionItem1.GetType();

                                    if (CanDirectlyCompare(collectionItemType))
                                    {
                                        if (!AreValuesEqual(collectionItem1, collectionItem2))
                                        {
                                            if (debug)
                                                Console.WriteLine(
                                                    "Item {0} in property collection '{1}.{2}' does not match.", i,
                                                    objectType.FullName, propertyInfo.Name);
                                            result = false;
                                        }
                                    }
                                    else if (!AreObjectsEqual(collectionItem1, collectionItem2, debug, ignoreList))
                                    {
                                        if (debug)
                                            Console.WriteLine(
                                                "Item {0} in property collection '{1}.{2}' does not match.", i,
                                                objectType.FullName, propertyInfo.Name);
                                        result = false;
                                    }
                                }
                            }
                        }
                    }
                    else if (propertyInfo.PropertyType.IsClass)
                    {
                        if (!AreObjectsEqual(propertyInfo.GetValue(objectA, null), propertyInfo.GetValue(objectB, null),
                            debug, ignoreList))
                        {
                            if (debug)
                                Console.WriteLine("Mismatch with property '{0}.{1}' found.", objectType.FullName,
                                    propertyInfo.Name);
                            result = false;
                        }
                    }
                    else
                    {
                        if (debug)
                            Console.WriteLine("Cannot compare property '{0}.{1}'.", objectType.FullName,
                                propertyInfo.Name);
                        result = false;
                    }
                }
            }
            else
                result = Equals(objectA, objectB);

            return result;
        }

        /// <summary>
        /// Determines whether value instances of the specified type can be directly compared.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// 	<c>true</c> if this value instances of the specified type can be directly compared; otherwise, <c>false</c>.
        /// </returns>
        private static bool CanDirectlyCompare(Type type)
        {
            return typeof(IComparable).IsAssignableFrom(type) || type.IsPrimitive || type.IsValueType;
        }

        /// <summary>
        /// Compares two values and returns if they are the same.
        /// </summary>
        /// <param name="valueA">The first value to compare.</param>
        /// <param name="valueB">The second value to compare.</param>
        /// <returns><c>true</c> if both values match, otherwise <c>false</c>.</returns>
        private static bool AreValuesEqual(object valueA, object valueB)
        {
            bool result;
            IComparable selfValueComparer;

            selfValueComparer = valueA as IComparable;

            if (valueA == null && valueB != null || valueA != null && valueB == null)
                result = false; // one of the values is null
            else if (selfValueComparer != null && selfValueComparer.CompareTo(valueB) != 0)
                result = false; // the comparison using IComparable failed
            else if (!Equals(valueA, valueB))
                result = false; // the comparison using Equals failed
            else
                result = true; // match

            return result;
        }
    }
}