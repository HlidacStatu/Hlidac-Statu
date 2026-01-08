using Devmasters;
using System.Text.RegularExpressions;

namespace HlidacStatu.Searching
{
    public static class Tools
    {
        public const int MaxResultWindow = 10000;

        public static readonly string[] DefaultQueryOperators = new string[] { "AND", "OR" };


        static string regexInvalidQueryTemplate = @"(^|\s|[(])(?<q>$operator$\s{1} (?<v>(\w{1,})) )($|\s|[)])";

        static RegexOptions regexQueryOption =
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline;

        public static string FixInvalidQuery(string query, IRule[] rules, string[] operators = null)
        {
            return FixInvalidQuery(query, rules.SelectMany(m => m.Prefixes).ToArray(), operators);
        }

        public static string FixInvalidQuery(string query, string[] shortcuts, string[] operators = null)
        {
            if (operators == null)
                operators = DefaultQueryOperators;

            if (string.IsNullOrEmpty(query))
                return query;

            string newquery = query;

            //fix query ala (issues.issueTypeId:18+OR+issues.issueTypeId:12)+ico:00283924
            if (!string.IsNullOrEmpty(query))
            {
                query = query.Trim();
                if (query.Contains("+") && !query.Contains(" "))
                    newquery = System.Net.WebUtility.UrlDecode(query);
            }

            MatchEvaluator evalDelimiterMatch = (m) =>
            {
                var s = m.Value;
                if (string.IsNullOrEmpty(s))
                    return string.Empty;
                if (m.Groups["v"].Success)
                {
                    var v = m.Groups["v"]?.Value?.ToUpper()?.Trim() ?? "";
                    if (operators.Contains(v))
                        return s;
                }

                var newVal = s.Replace(": ", ":");
                return newVal;
            };

            foreach (var qo in shortcuts)
            {
                string lookFor = regexInvalidQueryTemplate.Replace("$operator$", qo);
                //if (modifiedQ.ToLower().Contains(lookFor.ToLower()))
                if (Regex.IsMatch(newquery, lookFor, regexQueryOption))
                {
                    newquery = Regex.Replace(newquery, lookFor, evalDelimiterMatch, regexQueryOption);
                }
            }

            // [\p{L}\p{M}\d_] \p{L} -znaky všech abeced \p{M} -kombinované znaky
            string invalidFormatRegex = @"((AND|OR|NOT) \s+)? # mozny operator pred

 ([^""]\w +\.?\w +) # operator 
 :
 
 ( $ #konec text
   | #nebo
   [^\""=><[\p{L}\p{M}\d_{)(] #jiny znak je je zde na seznamu
 ) 
 
 ((AND|OR|NOT) )?  #mozny operator
";
            Match mIsInvalid = Regex.Match(query, invalidFormatRegex,
                (RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase));
            if (mIsInvalid.Success)
            {
                newquery = Regex.Replace(newquery, invalidFormatRegex, " ",
                    RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase).Trim();
            }

            var textParts = TextUtil.SplitStringToPartsWithQuotes(newquery, '\"');
            //make operator UpperCase and space around '(' and ')'
            if (textParts.Count > 0)
            {
                string fixedOperator = "";
                foreach (var tp in textParts)
                {
                    if (tp.Item2 == true)
                        fixedOperator = fixedOperator + tp.Item1;
                    else
                    {
                        var fixPart = tp.Item1;
                        fixPart = System.Net.WebUtility.UrlDecode(fixPart);
                        fixPart = System.Net.WebUtility.HtmlDecode(fixPart);
                        Regex opReg =
                            new Regex($@"(^|\s)({string.Join("|", operators)})(\s|$)",
                                regexQueryOption);

                        //UPPER Operator
                        fixPart = opReg.Replace(fixPart, (me) => { return me.Value.ToUpper(); });

                        //Space around '(' and ')'
                        fixPart = fixPart.Replace("(", "( ").Replace(")", " )");
                        fixedOperator = fixedOperator + fixPart;
                    }
                }

                newquery = fixedOperator;
            }

            //fix DÚK/Sou/059/2009  -> DÚK\\/Sou\\/059\\/2009
            //
            // but support regex name:/joh?n(ath[oa]n)/
            //
            //if (newquery.Contains("/")) //&& regFindRegex.Match(query).Success == false)
            //{
            //    newquery = newquery.Replace("/", "\\/");
            //}

            //fix with small "to" in zverejneno:[2018-12-13 to *]
            var dateintervalRegex =
                @"(podepsano|zverejneno):({|\[)\d{4}\-\d{2}\-\d{2}(?<to> \s*to\s*)(\d{4}-\d{2}-\d{2}|\*)(}|\])";
            if (Regex.IsMatch(newquery, dateintervalRegex, regexQueryOption))
            {
                newquery = newquery.ReplaceGroupMatchNameWithRegex(dateintervalRegex, "to", " TO ");
            }


            if (newquery != query)
                return newquery;
            else
                return query;
        }


    }
}
