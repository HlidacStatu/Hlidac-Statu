using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace HlidacStatu.Util
{
    public static class ParseTools
    {

        private const string DolozkaText = "Doložka konverze do dokumentu obsaženého v datové zprávě";

        public static bool EnoughExtractedTextCheck(long words, long lengthChars, long uniqueWordsCount, decimal wordsVariance)
        {

            return !(lengthChars <= 20 || words <= 10 ||
                (
                    uniqueWordsCount < 7 || (uniqueWordsCount >= 7 && wordsVariance > 0.7m)
                )

            );
        }
        public static int RomanToLatin(string roman)
        {
            Dictionary<char, int> romanToLatin = new Dictionary<char, int>()
                {
                    {'I', 1},
                    {'V', 5},
                    {'X', 10},
                    {'L', 50},
                    {'C', 100},
                    {'D', 500},
                    {'M', 1000}
                };

            int result = 0;
            int previousValue = 0;

            for (int i = roman.Length - 1; i >= 0; i--)
            {
                int currentValue = romanToLatin[roman[i]];

                if (currentValue < previousValue)
                {
                    result -= currentValue;
                }
                else
                {
                    result += currentValue;
                }

                previousValue = currentValue;
            }

            return result;
        }

        public static bool EnoughExtractedTextCheck(string plaintext, int pages)
        {
            pages = pages - 2;

            if (pages <= 1)
                pages = 1;

            if (string.IsNullOrEmpty(plaintext))
                return false;
            
            string textWithTrimmedSpaces = Regex.Replace(plaintext, @"\s+", " ");
            var penalization = DolozkaPenalization(textWithTrimmedSpaces);
            
            
            var wordsPerPage = Devmasters.TextUtil.CountWords(textWithTrimmedSpaces);
            wordsPerPage -= penalization.WordCount;
            wordsPerPage = wordsPerPage / pages;
            
            var lengthPerPage = textWithTrimmedSpaces?.Length ?? 0;
            lengthPerPage -= penalization.Length;
            lengthPerPage = lengthPerPage / pages;
            
            var variance = Devmasters.TextUtil.WordsVarianceInText(textWithTrimmedSpaces);
            var uniqueWordsCount = variance.Item2;
            var wordsVariance = variance.Item1; //0 = kazde slovo jine, 1=vsechna stejna

            return EnoughExtractedTextCheck(wordsPerPage, lengthPerPage, uniqueWordsCount, wordsVariance);
        }

        public static (int Length, int WordCount) DolozkaPenalization(string plaintext)
        {
            var matchCount = Regex.Matches(plaintext, DolozkaText,
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase).Count;
            return (matchCount * 470, matchCount * 63);
        }


        /// <summary>
        /// support simple LIKE syntaxt from T-SQL 
        /// %asdf 
        /// asdf%
        /// %asdf%
        /// </summary>
        public static bool FindInStringSqlLike(string wholetext, string stringToFind, StringComparison sc = StringComparison.OrdinalIgnoreCase)
        {
            if (stringToFind.StartsWith("%") && stringToFind.EndsWith("%"))
                return wholetext.IndexOf(stringToFind.Substring(1, stringToFind.Length - 2), sc) >= 0;
            else if (stringToFind.StartsWith("%"))
                return wholetext.EndsWith(stringToFind.Substring(1, stringToFind.Length - 1), sc);
            else if (stringToFind.EndsWith("%"))
                return wholetext.StartsWith(stringToFind.Substring(0, stringToFind.Length - 1), sc);
            else
                return string.Equals(wholetext, stringToFind, sc);


        }

        static Dictionary<string, string> FunkceNormalizaction = new Dictionary<string, string>()
        {
            {"poslanec%","poslanec" },
            {"poslankyně%","poslanec" },
            {"místopředsed%","místopředseda" },
            {"předsed%","předseda" },
            {"senátor%","senátor" },
            {"evropská komisařk%","komisař eu" },
            {"evropský komisař","komisař eu" },
            {"komisař%","komisař eu" },
            {"viceprezident%","viceprezident" },
            {"prezident%","prezident" },
            {"ministr%","ministr" },
            {"místopředsedkyn%","místopředseda" },
            {"starost%","starosta" },
            {"hejtman%","hejtman" },
            {"primátor%","primátor" },
            {"guvernér%","guvernér" },
            {"ředitel%","ředitel" },
            {"náměstkyn%","náměstek" },
            {"náměstek","náměstek" },
            {"%zastupitel%","zastupitel" },
        };

        public static string NormalizePolitikFunkce(string fce)
        {
            foreach (var fkv in FunkceNormalizaction)
            {
                if (FindInStringSqlLike(fce, fkv.Key))
                    return fkv.Value;
            }
            return fce;
        }

        public static string NormalizeIco(string ico)
        {
            if (ico == null)
                return string.Empty;

            ico = ico.Trim().ToLower();
            
            if (string.IsNullOrEmpty(ico))
                return string.Empty;

            if (ico.StartsWith("cz-"))
                return MerkIcoToICO(ico);


            if (DataValidators.IsFirmaIcoZahranicni(ico))
                return ico;
            
            ico = Devmasters.TextUtil.NormalizeToNumbersOnly(ico);
            if (string.IsNullOrEmpty(ico))
                return string.Empty;

            return ico.PadLeft(8, '0');
            
        }


        public static string MerkIcoToICO(string merkIco)
        {
            if (merkIco.ToLower().Contains("cz-"))
                merkIco = merkIco.Trim().Replace("cz-", "");

            return merkIco.PadLeft(8, '0');
        }
        public static string IcoToMerkIco(string ico)
        {
            if (Devmasters.TextUtil.IsNumeric(ico))
            {
                long tmp;
                if (long.TryParse(ico.Trim(), out tmp))
                {
                    return tmp.ToString();
                }
            }

            return ico;
        }


        public static string NormalizePrilohaPlaintextText(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            //\u0000
            string ns = s.Replace("\0", "");

            //remove /n/r on the beginning
            ns = System.Text.RegularExpressions.Regex.Replace(ns, "^(\\s)*", "");
            ns = Devmasters.TextUtil.ReplaceDuplicates(ns, "\n\n");
            ns = Devmasters.TextUtil.ReplaceDuplicates(ns, "\r\r");
            ns = Devmasters.TextUtil.ReplaceDuplicates(ns, "\t\t");
            ns = ns.Trim();

            //remove /n/r on the beginning again
            ns = System.Text.RegularExpressions.Regex.Replace(ns, "^(\\s)*", "");
            return ns;
        }

        static Dictionary<string, string> StranyZkratky = new Dictionary<string, string>()
        {
            {"kdu","KDU-ČSL" },
            {"*ceskoslovenska strana lidova*","KDU-ČSL" },

            { "sz","Strana zelených" },
            { "zeleni","Strana zelených" },
            { "strana zelenych","Strana zelených" },

            {"starostove a nezavisli","STAN" },

            {"top09","TOP 09" },
            {"pirati","Česká pirátská strana" },
            {"ceska strana socialne demokraticka","ČSSD" },

            {"ano2011","ANO" },
            {"ano","ANO" },
            {"hnuti ano","ANO" },
            {"ano 2011, o.s.","ANO" },
            {"ano 2011","ANO" },

            {"obcanska demokraticka strana","ODS" },

            {"spo","Strana Práv Občanů ZEMANOVCI" },
            {"spoz","Strana Práv Občanů ZEMANOVCI" },
            {"strana prav obcanu","Strana Práv Občanů ZEMANOVCI" },
            {"strana prav obcanu zemanovci","Strana Práv Občanů ZEMANOVCI" },

            {"svoboda a prima demokracie","SPD" },
            {"spd","SPD" },
            {"svoboda a prima demokracie*","SPD" },

            {"strana svobodnych obcanu","Svobodní" },
            {"usvit - narodni koalice","Úsvit" },
            {"usvit-narodni koalice","Úsvit" },

            {"komunisticka strana cech a moravy","KSČM" },
            {"lev 21","Národní socialisté" },
            {"*rozumni*","ROZUMNÍ" },
            {"vv","Věci veřejné" },
            {"veci verejne","Věci veřejné" },
};

        public static string NormalizaceStranaShortName(string strana)
        {
            if (string.IsNullOrWhiteSpace(strana))
                return string.Empty;

            var s = strana.ToLower();
            s = Devmasters.TextUtil.ReplaceDuplicates(s, ' ');
            s = Devmasters.TextUtil.RemoveDiacritics(s);

            string[] vals = StranyZkratky.Keys
                    //.Union(StranyZkratky
                    //            .Values
                    //            .Select(m => Devmasters.TextUtil.RemoveDiacritics(m).ToLower())
                    //            .Distinct()
                    //            )
                    .ToArray();

            foreach (var val in vals)
            {
                if (val.StartsWith("*") && val.EndsWith("*"))
                {
                    if (s.Contains(val.Replace("*", "")))
                        return StranyZkratky[val];
                }
                if (val.StartsWith("*"))
                {
                    if (s.EndsWith(val.Replace("*", "")))
                        return StranyZkratky[val];
                }
                if (val.EndsWith("*"))
                {
                    if (s.StartsWith(val.Replace("*", "")))
                        return StranyZkratky[val];
                }
                else if (s == val)
                    return StranyZkratky[val];
            }
            return strana;

        }

        public static int? ToInt(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;
            int ret = 0;
            if (int.TryParse(value, out ret))
            {
                return ret;
            }
            else
                return null;
        }

        public static decimal? FromTextToDecimal(string value)
        {
            return ToDecimal(Devmasters.TextUtil.NormalizeToNumbersOnly(value));
        }

        public static decimal? ToDecimal(string value)
        {
            if (string.IsNullOrEmpty(value))
                return null;

            value = value.Replace(" ", "").Trim();
            if (value.StartsWith(",") || value.StartsWith("."))
                value = value.Substring(1);
            if (value.EndsWith(",") || value.EndsWith("."))
                value = value.Substring(0, value.Length - 1);
            if (value.EndsWith(",-") || value.EndsWith(".-"))
                value = value.Substring(0, value.Length - 2);
            if (value.EndsWith(",--") || value.EndsWith(".--"))
                value = value.Substring(0, value.Length - 3);
            if (value.EndsWith(",00") || value.EndsWith(".00"))
                value = value.Substring(0, value.Length - 3);

            //System.Globalization.NumberStyles styles = System.Globalization.NumberStyles.AllowLeadingWhite
            //    | System.Globalization.NumberStyles.AllowTrailingWhite
            //    | System.Globalization.NumberStyles.AllowThousands
            //    | System.Globalization.NumberStyles.AllowCurrencySymbol
            //    ;
            if (decimal.TryParse(value, System.Globalization.NumberStyles.Any, Consts.czCulture, out decimal tmp)
                || decimal.TryParse(value, System.Globalization.NumberStyles.Any, Consts.enCulture, out tmp))
                return tmp;
            else
                return null;
        }


        public static T ChangeType<T>(object value)
        {
            var t = typeof(T);

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return default(T);
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return (T)Convert.ChangeType(value, t);
        }

        public static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                if (Nullable.GetUnderlyingType(t) == typeof(DateOnly))
                {
                    if (DateOnly.TryParseExact((string)value, "yyyy-MM-dd",
                        Consts.enCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateOnly date))
                    {
                        return date;
                    }
                    else
                        return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            if (t == typeof(DateOnly))
            {
                return DateOnly.ParseExact((string)value, "yyyy-MM-dd",
                    Consts.enCulture, System.Globalization.DateTimeStyles.AssumeLocal);
            }
            return Convert.ChangeType(value, t);
        }

        public static (string queryString, string queryTagLengths) CreateQueryWithOffsets(List<string> input)
        {
            string qs = "";
            string qtl = "";

            for (var index = 0; index < input.Count; index++)
            {
                var item = input[index];

                if (index == input.Count - 1)
                {
                    qtl += $"{item.Length}";
                    qs += $"{item}";    
                }
                else
                {
                    qtl += $"{item.Length},";
                    qs += $"{item} ";
                }
            }

            return (qs, qtl);
        }
        
        public static List<string> ParseQueryStringWithOffsets(string queryString, string lengthsString)
        {
            if (string.IsNullOrWhiteSpace(queryString) || string.IsNullOrWhiteSpace(lengthsString))
                return null;
            
            var offsets = lengthsString.Split(",",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            List<string> results = new();

            int previousOffset = 0;
            foreach (var offsetText in offsets)
            {
                if(!int.TryParse(offsetText, out int tagLength))
                    continue;
                
                results.Add(queryString.Substring(previousOffset, tagLength));
                
                // +1 here is a space between tags
                previousOffset += tagLength + 1;

            }

            return results;
        }
        
        public static List<string> ParseQueryStringWithoutOffsets(string queryString)
        {
            if (string.IsNullOrWhiteSpace(queryString))
                return null;

            return queryString.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }

    }
}