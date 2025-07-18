﻿using Devmasters.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Html;

namespace HlidacStatu.Util
{
    public static partial class RenderData
    {
        public enum CapitalizationStyle
        {
            AllUpperCap,
            EveryWordUpperCap,
            FirstLetterUpperCap,
            AllLowerCap,
            NoChange,
        }

        //public static string RenderInfoFacts(this IEnumerable<InfoFact> infofacts, int number,
        //    bool takeSummary = true, bool shuffle = false,
        //    string delimiterBetween = " ",
        //    string lineFormat = "{0}", bool html = false)
        //{
        //    return InfoFact.RenderFacts(infofacts.ToArray(), number, takeSummary, shuffle, delimiterBetween, lineFormat, html);
        //}

        public static string GetIntervalString(DateTime from, DateTime to)
        {
            string sFrom = "";
            string sTo = "";
            if (from == from.Date)
                sFrom = from.ToString("d.M.");
            else
                sFrom = from.ToString("d.M. HH:mm");

            if (to == to.Date)
                sTo = to.ToString("d.M.");
            else
                sTo = to.ToString("d.M. HH:mm");


            return string.Format("od {0} do {1} ", sFrom, sTo);
        }

        public static string Random(params string[] texts)
        {
            if (texts == null)
                return string.Empty;
            else if (texts.Length == 0)
                return string.Empty;
            else if (texts.Length == 1)
                return texts[0];
            else
            {
                return texts[Consts.Rnd.Next(texts.Length)];
            }
        }
        public static string Capitalize(string s, CapitalizationStyle style)
        {
            if (string.IsNullOrWhiteSpace(s))
                return s;

            switch (style)
            {
                case CapitalizationStyle.AllUpperCap:
                    return s.ToUpperInvariant();
                case CapitalizationStyle.EveryWordUpperCap:
                    return System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
                case CapitalizationStyle.FirstLetterUpperCap:
                    return s.First().ToString().ToUpper() + s.Substring(1);
                case CapitalizationStyle.AllLowerCap:
                    return s.ToLowerInvariant();
                case CapitalizationStyle.NoChange:
                default:
                    return s;
            }
        }



        public static string RenderCharWithCH(char c)
        {
            if (c == Consts.Ch)
                return "Ch";
            else
                return c.ToString();
        }

        public static string NumberOfResults(long value)
        {
            return NiceNumber(value);
        }

        public static string EmailAnonymizer(string email)
        {
            //todo more validations
            Regex mr = new Regex(@"(?<prefix>.*)@(?<mid>(.|\w)*) (?<end>\. .*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace | RegexOptions.CultureInvariant);
            Match mc = mr.Match(email);
            string pref = mc.Groups["prefix"].Value;
            string mid = mc.Groups["mid"].Value;
            string end = mc.Groups["end"].Value;
            if (mc.Success)
            {
                return pref + "@"
                    + mid.Substring(0, 1) + "..."
                    + mid.Substring(mid.Length - 1, 1)
                    + end;
            }
            else
                return email;

        }




        public static string NicePrice(decimal number, string valueIfZero = "0 {0}", string mena = "Kč", bool html = false, bool shortFormat = false, ShowDecimalVal showDecimal = ShowDecimalVal.Hide)
        {

            string s = string.Empty;
            if (shortFormat)
            {
                return ShortNicePrice(number, valueIfZero, mena, html, showDecimal);
            }
            else
                return ShortNicePrice(number, valueIfZero, mena, html, showDecimal, MaxScale.Jeden);
        }

        public static IHtmlContent NicePriceHtml(decimal number, string valueIfZero = "0 {0}", string mena = "Kč", bool shortFormat = false, ShowDecimalVal showDecimal = ShowDecimalVal.Hide)
        {
            var result = shortFormat
                ? ShortNicePrice(number, valueIfZero, mena, html: true, showDecimal)
                : ShortNicePrice(number, valueIfZero, mena, html: true, showDecimal, MaxScale.Jeden);

            return new HtmlString(result);

        }

        static string tableOrderValueFormat = "000000000000000#";
        public static string OrderValueFormat(string n) { return n; }
        public static string OrderValueFormat(double? n) { return ((n ?? 0) * 1000000).ToString(tableOrderValueFormat); }
        public static string OrderValueFormat(decimal? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(byte? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(float? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(int? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(long? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(short? n) { return OrderValueFormat((double?)n); }
        public static string OrderValueFormat(DateTime? n) { return OrderValueFormat(n.HasValue ? n.Value.Ticks : 0); }
        public static string OrderValueFormat(TimeSpan n) { return OrderValueFormat(n.Ticks); }

        public static MaxScale GetBestScale(IEnumerable<double> numbers) => GetBestScale(numbers.Cast<decimal>());
        public static MaxScale GetBestScale(IEnumerable<int> numbers) => GetBestScale(numbers.Cast<decimal>());
        public static MaxScale GetBestScale(IEnumerable<float> numbers) => GetBestScale(numbers.Cast<decimal>());

        public static MaxScale GetBestScale(IEnumerable<decimal> numbers)
        {
            //logika: pokud je druhy nej rad zastoupen pod 10%, beru rad nejcastejsi
            // pokud u tri+ radu maji 2. a 3. pod 20%, beru nejcastejsi
            // pokud u tri+ ma jeden nad 10%, beru ten

            double threshold = 0.1d;

            if (numbers == null)
                throw new ArgumentNullException();
            if (numbers.Count() == 0)
                return MaxScale.Any;

            var stat = numbers
                //.Select(n=>new { sc = GetBestScale(n) })
                .GroupBy(k => GetBestScale(k), v => GetBestScale(v), (v, k) => new { sc = v, num = (double)k.Count() })
                .OrderByDescending(o => o.num)
                .ToArray();

            if (stat.Count() == 1)
                return stat[0].sc;
            else if (stat.Count() == 2)
            {
                double sum = stat.Sum(m => m.num);
                var secCount = stat[1].num;
                if (secCount / sum > threshold && stat[0].sc < stat[1].sc)
                    return stat[1].sc;
                else
                    return stat[0].sc;
            }
            else //if (stat.Count() => 3)
            {
                double sum = stat.Sum(m => m.num);
                var top = stat[0];

                var rest = stat.Select(m => new { sc = m.sc, perc = m.num / sum })
                    .OrderByDescending(o => o.perc)
                    .Skip(1);

                if (rest.Any(m => m.perc > threshold && m.sc < top.sc))
                    return rest.Where(m => m.perc > threshold && m.sc < top.sc).Min(m => m.sc);
                else
                    return top.sc;
            }
            //else return stat.Min(m => m.sc);

        }

        static decimal OneTh = 1000;
        static decimal OneMil = OneTh * 1000;
        static decimal OneMld = OneMil * 1000;
        static decimal OneBil = OneMld * 1000;
        private static MaxScale GetBestScale(decimal number)
        {

            if (number >= OneBil)
                return MaxScale.Bilion;
            else if (number >= OneMld)
                return MaxScale.Miliarda;
            else if (number >= OneMil)
                return MaxScale.Milion;
            else if (number >= OneTh)
                return MaxScale.Tisic;
            else
                return MaxScale.Jeden;
        }

        [ShowNiceDisplayName()]
        public enum MaxScale
        {
            [NiceDisplayName("")]
            Jeden = 1,
            [NiceDisplayName("tis.")]
            Tisic = 3,
            [NiceDisplayName("mil.")]
            Milion = 6,
            [NiceDisplayName("mld.")]
            Miliarda = 9,
            [NiceDisplayName("bil.")]
            Bilion = 12,

            [NiceDisplayName("")]
            Any = 99
        }

        public static string ShortNiceNumber(decimal number,
            bool html = false,
            ShowDecimalVal showDecimal = ShowDecimalVal.AsNeeded,
            MaxScale exactScale = MaxScale.Any,
            bool hideSuffix = false)
        {
            return ShortNicePrice(number, "0", "", html, showDecimal, exactScale, hideSuffix);
        }

        public static string ShortNicePrice(decimal number,
            string valueIfZero = "0 {0}", string mena = "Kč", bool html = false,
            ShowDecimalVal showDecimal = ShowDecimalVal.Hide,
            MaxScale exactScale = MaxScale.Any,
            bool hideSuffix = false)
        {

            decimal n = number;

            string suffix;
            if ((n > OneBil && exactScale == MaxScale.Any) || exactScale == MaxScale.Bilion)
            {
                n /= OneBil;
                suffix = "bil.";
            }
            else if ((n > OneMld && exactScale == MaxScale.Any) || exactScale == MaxScale.Miliarda)
            {
                n /= OneMld;
                suffix = "mld.";
            }
            else if ((n > OneMil && exactScale == MaxScale.Any) || exactScale == MaxScale.Milion)
            {
                n /= OneMil;
                suffix = "mil.";
            }
            else if (exactScale == MaxScale.Tisic)
            {
                n /= OneTh;
                suffix = "";
            }
            else
            {
                suffix = "";
            }

            if (hideSuffix)
                suffix = "";

            string ret = string.Empty;

            string formatString = "{0:### ### ### ### ### ##0} " + suffix + " {1}";
            if (showDecimal == ShowDecimalVal.Show)
                formatString = "{0:### ### ### ### ### ##0.00} " + suffix + " {1}";
            else if (showDecimal == ShowDecimalVal.AsNeeded)
                formatString = "{0:### ### ### ### ### ##0.##} " + suffix + " {1}";


            if (number == 0)
            {
                if (valueIfZero.Contains("{0}"))
                    ret = string.Format(valueIfZero, mena);
                else
                    ret = valueIfZero;
            }
            else
            {
                ret = String.Format(formatString, n, mena).Trim();
            }

            ret = ret.Trim();


            if (html)
            {
                return String.Format("<span title=\"{2:### ### ### ### ### ##0} {1}\">{0}</span>",
                    Devmasters.TextUtil.ReplaceDuplicates(ret, ' ').Replace(" ", "&nbsp;"), mena, number);

            }
            return ret;


        }

        public enum ShowDecimalVal
        {
            Hide = 0,
            Show = 1,
            AsNeeded = -1,
        }
        public static string NicePercent(decimal? number, int decimalPlaces = 2, bool html = false, string noValue = "")
        {
            if (number.HasValue == false)
                return noValue;

            var s = number.Value.ToString("P" + decimalPlaces);

            if (html)
            {
                return String.Format("<span title=\"{1}\">{0}</span>",
                    Devmasters.TextUtil.ReplaceDuplicates(s, ' ').Replace(" ", "&nbsp;"), s);

            }
            return s;

        }

        public static string NiceNumber(decimal number, bool html = false, ShowDecimalVal showDecimal = ShowDecimalVal.AsNeeded)
        {
            return ShortNiceNumber(number, html, showDecimal, MaxScale.Jeden, hideSuffix: true);
        }

        public static string NiceNumber(long number, bool html = false, ShowDecimalVal showDecimal = ShowDecimalVal.AsNeeded)
            => NiceNumber((decimal)number, html, showDecimal);

        public static string TextToHtml(string txt)
        {
            return txt.Replace("\n", "<br/>");
        }

        public static string ToDate(DateTime? date, string format = null)
        {
            if (date.HasValue == false)
                return string.Empty;

            format = format ?? "d. M. yyyy";
            switch (date.Value.Kind)
            {
                case DateTimeKind.Unspecified:
                case DateTimeKind.Local:
                    return date.Value.ToUniversalTime().ToString(format);
                case DateTimeKind.Utc:
                    return date.Value.ToString(format);
                default:
                    return date.Value.ToUniversalTime().ToString(format);
            }
        }

        public static string ToElasticDate(DateTime date)
        {
            switch (date.Kind)
            {
                case DateTimeKind.Unspecified:
                case DateTimeKind.Local:
                    return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                case DateTimeKind.Utc:
                    return date.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
                default:
                    return date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            }
        }

        public static string ToJavascriptDateUTC(DateTime date)
        {
            DateTime m = date;
            switch (date.Kind)
            {
                case DateTimeKind.Unspecified:
                case DateTimeKind.Local:
                    m = date.ToUniversalTime();
                    break;
                case DateTimeKind.Utc:
                    m = date;
                    break;
                default:
                    m = date.ToUniversalTime();
                    break;
            }

            return $"Date.UTC({m.Date.Year},{ m.Date.Month - 1},{ m.Date.Day})";
        }

        public static string RenderList(IEnumerable<string> data, string format = "{0}",
            string itemsDelimiter = ", ", string lastItemDelimiter = " a ", string ending = "."
            )
        {
            return RenderList(
                data.Select(m => new string[] { m })
                , format, itemsDelimiter, lastItemDelimiter, ending);
        }
        public static string RenderList(IEnumerable<string[]> data, string format = "{0}",
        string itemsDelimiter = ", ", string lastItemDelimiter = " a ", string ending = "."
        )
        {
            if (data == null)
                return string.Empty;
            if (data.Count() == 0)
                return string.Empty;

            var aData = data.ToArray();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append(
                string.Join(itemsDelimiter, aData
                                .Take(aData.Length - 1)
                                .Select(m => string.Format(format, m))
                                )
                );
            if (aData.Length > 1)
                sb.Append(lastItemDelimiter);
            sb.Append(string.Format(format, aData.Last()));
            sb.Append(ending);
            return sb.ToString();
        }
        public static string LimitedList(int maxItems, IEnumerable<string> data, string format = "{0}",
            string itemsDelimiter = "\n", string lastItemDelimiter = "\n",
            string moreTextPrefix = null, Devmasters.Lang.CS.PluralDef morePluralForm = null,
            bool moreNumberFormat = true
            )
        {
            return LimitedList(maxItems, data.Select(m => new string[] { m }), format, itemsDelimiter, lastItemDelimiter, moreTextPrefix, morePluralForm, moreNumberFormat);
        }
        public static string LimitedList(int maxItems, IEnumerable<string[]> data, string format = "{0}",
           string itemsDelimiter = "\n", string lastItemDelimiter = "\n",
           string moreTextPrefix = null, Devmasters.Lang.CS.PluralDef morePluralForm = null,
           bool moreNumberFormat = true
           )
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string more = "";
            if (data.Count() == maxItems + 1)
            {
                sb.Append(RenderList(data.Take(maxItems + 1), format, itemsDelimiter, lastItemDelimiter, ""));
                maxItems = data.Count();
            }
            else
                sb.Append(RenderList(data.Take(maxItems), format, itemsDelimiter, lastItemDelimiter, ""));

            if (data.Count() > maxItems)
            {
                int diff = data.Count() - maxItems;
                string sDiff = diff.ToString();
                if (moreNumberFormat)
                    sDiff = NiceNumber(diff);

                if (!string.IsNullOrEmpty(moreTextPrefix))
                {
                    if (moreTextPrefix.Contains("{0}"))
                        more = string.Format(moreTextPrefix, sDiff);
                    else
                        more = moreTextPrefix;
                }
                if (morePluralForm != null)
                    more = more + Devmasters.Lang.CS.Plural.Get(diff, morePluralForm);

            }
            sb.Append(more);
            return sb.ToString();
        }

        public static string ChangeValueSymbol(decimal change, bool html)
        {

            string symbol = "";
            if (-0.001m < change && change < 0.001m)
                symbol = "↔";
            else if (change <= -0.001m)
                symbol = "↓";
            else
                symbol = "↑";
            if (html)
            {
                if (symbol == "↓")
                    return $"<span class=\"text-danger\">{change.ToString("P2")}&nbsp;&darr;</span>";
                else if (symbol == "↑")
                    return $"<span class=\"text-success\">{change.ToString("P2")}&nbsp;&uarr;</span>";
                else
                    return $"<span class=\"\">{change.ToString("P2")}&nbsp;=</span>";
            }
            else
            {
                return $"{change.ToString("P2")} {symbol}";
            }
        }

        public static string ChangeValueText(decimal change, bool html,
            string decreaseTxt = "pokles o {0:P2}",
            string equallyTxt = "bezezměny",
            string increaseTxt = "nárůst o {0:P2}"
            )
        {
            if (-0.001m < change && change < 0.001m)
                return equallyTxt.Contains("{0") ? string.Format(equallyTxt, change) : equallyTxt;
            else if (change <= -0.001m)
                return decreaseTxt.Contains("{0") ? string.Format(decreaseTxt, change) : decreaseTxt;
            else
                return increaseTxt.Contains("{0") ? string.Format(increaseTxt, change) : increaseTxt;
        }

        public static string TimeSpanToDaysHoursMinutes(TimeSpan timeSpan, bool withSeconds = false)
        {
            int days = timeSpan.Days;
            int hours = timeSpan.Hours;
            int minutes = timeSpan.Minutes;
            int seconds = timeSpan.Seconds;

            var parts = new List<string>();

            if (days > 0)
                parts.Add(Devmasters.Lang.CS.Plural.Get(days, "jeden den", "{0} dny", "{0} dnů"));

            if (hours > 0)
                parts.Add(Devmasters.Lang.CS.Plural.Get(hours, "jedna hodina", "{0} hodiny", "{0} hodin"));

            if (minutes > 0)
                parts.Add(Devmasters.Lang.CS.Plural.Get(minutes, "jedna minuta", "{0} minuty", "{0} minut"));

            if (withSeconds && seconds > 0)
                parts.Add(Devmasters.Lang.CS.Plural.Get(seconds, "1 s", "{0} s", "{0} s"));

            return parts.Count > 0 ? string.Join(", ", parts) : "méně než minuta";
        }


        const string accentedCharacters = "àèìòùÀÈÌÒÙáéíóúýÁÉÍÓÚÝâêîôûÂÊÎÔÛãñõÃÑÕäëïöüÿÄËÏÖÜŸçÇßØøÅåÆæœčČšŠřŘžŽťŤňŇďĎĺĹěĚúÚůŮ";
        static HashSet<string> stopWords = new Devmasters.Lang.CS.CzechStemmerAgressive().StopWords;
        static HashSet<string> stopWordsAscii = new Devmasters.Lang.CS.CzechStemmerAgressive().StopWords
            .Select(m=>Devmasters.TextUtil.RemoveDiacritics(m))
            .Distinct()
            .ToHashSet();

        public static string NormalizedTextNoStopWords(string text, bool removeOneCharWords, bool removeAccent)
        {
            if (text == null)
                return null;
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            string normText = Regex.Replace(text, "[^a-zA-Z0-9" + accentedCharacters + "'-]{1}", " ");
            if (removeOneCharWords)
            {
                normText = Regex.Replace(normText, @"(\s|^)[a-zA-Z0-9" + accentedCharacters + @"-]{1}(\s|$)", " ");
                //remove two char word like V1 or 1V
                normText = Regex.Replace(normText, @"(\s|^)[a-zA-Z0-9" + accentedCharacters + @"-]{1}\d{1}(\s|$)", " ");
                normText = Regex.Replace(normText, @"(\s|^)\d{1}[a-zA-Z0-9" + accentedCharacters + @"-]{1}(\s|$)", " ");
            }
            normText = Devmasters.TextUtil.ReplaceDuplicates(normText, " ");

            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            foreach (string w in normText.ToLower().Split(Devmasters.Lang.CS.Stemming.Separators, StringSplitOptions.RemoveEmptyEntries))
            {
                
                if (!stopWords.Contains(w))
                {
                    if (!stopWordsAscii.Contains(Devmasters.TextUtil.RemoveAccents(w)))
                        sb.Append(w + " ");
                }
            }
            normText = sb.ToString().Trim();
            if (removeAccent)
                return Devmasters.TextUtil.RemoveDiacritics(normText);
            return normText;
        }
    }
}

