using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace HlidacStatu.Searching
{
    public partial class SplittingQuery
    {
        [GeneratedRegex(@"(?<w>\w*) ~ (?<n>\d{0,2})",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline)]
        private static partial Regex TildePatternRegex();

        [GeneratedRegex(@"(^|\s|[(]) (?<p>(\w|\.)*:) (?<q>(-|\w)* )\s*",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace |
            RegexOptions.CultureInvariant)]
        public static partial Regex FindPrefixRegex();


        [DebuggerDisplay("{ToQueryString}")]
        public class Part
        {
            public string Prefix { get; set; } = "";
            public string Value { get; set; } = "";

            public bool ExactValue { get; set; } = false;

            public string ToQueryString
            {
                get { return ExportPartAsQuery(true); }
            }

            public string ExportPartAsQuery(bool encode = true)
            {
                //force not to encode
                // encode = false;
                if (ExactValue)
                    return Value;
                else
                {
                    if (encode)
                        return (Prefix ?? "") + EncodedValue();
                    else
                    {
                        return (Prefix ?? "") + Value;
                    }
                }
            }

            static readonly HashSet<char> reservedAll = new HashSet<char>
                { '+', '=', '!', '(', ')', '{', '}', '[', ']', '^', '\'', '~', '*', '?', ':', '\\', '/' };

            static char[] skipIfPrefix = new char[] { '-', '*', '?' };

            static char[] formulaStart = new char[] { '>', '<', '(', '{', '[' };
            static char[] formulaEnd = new char[] { ')', '}', ']', '*' };
            static char[] ignored = new char[] { '>', '<' };

            public string EncodedValue()
            {
                if (ExactValue)
                    return Value;
                if (string.IsNullOrWhiteSpace(Value))
                    return Value;

                //The reserved characters are:  + - = && || > < ! ( ) { } [ ] ^ " ~ * ? : \ /
                // https://www.elastic.co/guide/en/elasticsearch/reference/7.5/query-dsl-query-string-query.html
                //< and > can’t be escaped at all. The only way to prevent them from attempting to create a range query is to remove them from the query string entirely.


                var val = Value.Trim();
                if (formulaStart.Contains(val[0]) && formulaEnd.Contains(val[val.Length - 1]))
                    return val;

                if (val.EndsWith("*"))
                    return val;

                //allow ~ or ~5 on the end of word
                //replace ~ with chr(254)
                val = TildePatternRegex().Replace(val, "${w}\u00FE${n}");

                // Pre-allocate StringBuilder with estimated capacity
                StringBuilder sb = new StringBuilder(val.Length + 10);

                for (int i = 0; i < val.Length; i++)
                {
                    char currentChar = val[i];

                    // Handle character replacement for temporary marker
                    if (currentChar == (char)254)
                    {
                        sb.Append('~');
                        continue;
                    }

                    if (!string.IsNullOrEmpty(Prefix) && skipIfPrefix.Contains(currentChar))
                    {
                        sb.Append(currentChar);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(Prefix) && i == 0 && formulaStart.Contains(currentChar))
                    {
                        sb.Append(currentChar);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(Prefix) == false
                        && i == 0 && val.Length > 1
                        && formulaStart.Contains(currentChar) && val[i + 1] == '=')
                    {
                        sb.Append(currentChar);
                        sb.Append(val[i + 1]);
                        i = i + 1;
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(Prefix) && i == val.Length - 1 && formulaEnd.Contains(currentChar))
                    {
                        sb.Append(currentChar);
                        continue;
                    }

                    if ((i > 0) && ignored.Contains(currentChar))
                        continue;

                    if ((i > 0 || i < val.Length - 1) && reservedAll.Contains(currentChar))
                    {
                        sb.Append('\\');
                        sb.Append(currentChar);
                    }
                    else
                    {
                        sb.Append(currentChar);
                    }
                }

                return sb.ToString();
            }
        }

        public static SplittingQuery SplitQuery(string query)
        {
            return new SplittingQuery(query);
        }

        private SplittingQuery(string query)
        {
            _parts = Split(query);
        }

        public SplittingQuery()
            : this(new Part[] { })
        {
        }

        public SplittingQuery(Part[] parts)
        {
            _parts = parts ?? new Part[] { };
        }


        public string FullQuery()
        {
            if (_parts.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            foreach (var part in _parts)
            {
                var trimmed = part.ToQueryString.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    if (sb.Length > 0)
                        sb.Append(" ");
                    sb.Append(trimmed);
                }
            }

            return sb.ToString().Trim();
        }

        Part[] _parts = null;

        public Part[] Parts
        {
            get { return _parts; }
        }

        public void AddParts(Part[] parts)
        {
            var p = new List<Part>(_parts);
            p.AddRange(parts);
            _parts = p.ToArray();
        }

        public void InsertParts(int index, Part[] parts)
        {
            var p = new List<Part>(_parts);
            p.InsertRange(index, parts);
            _parts = p.ToArray();
        }

        public void ReplaceWith(int index, Part[] parts)
        {
            var p = new List<Part>(_parts);
            p.RemoveAt(index);
            p.InsertRange(index, parts);
            _parts = p.ToArray();
        }


        private Part[] Split(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            List<Part> tmpParts = new List<Part>();
            //prvni rozdelit podle ""
            var fixTxts = Devmasters.TextUtil.SplitStringToPartsWithQuotes(query, '\"');


            //spojit a rozdelit podle mezer
            for (int i = 0; i < fixTxts.Count; i++)
            {
                //fixed string
                if (fixTxts[i].Item2)
                {
                    if (i == 0)
                    {
                        tmpParts.Add(new Part()
                            {
                                ExactValue = true,
                                Value = fixTxts[i].Item1
                            }
                        );
                    }
                    else if (i > 0 && fixTxts[i - 1].Item1.EndsWith(":"))
                    {
                        tmpParts[tmpParts.Count - 1].Prefix = tmpParts[tmpParts.Count - 1].Prefix;
                        tmpParts[tmpParts.Count - 1].Value = fixTxts[i].Item1;
                    }
                    else
                    {
                        tmpParts.Add(new Part()
                            {
                                ExactValue = true,
                                Value = fixTxts[i].Item1
                            }
                        );
                    }
                }
                else
                {
                    //string[] mezery = fixTxts[i].Item1.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string tPart = fixTxts[i].Item1;
                    tPart = tPart.Replace("(", " ( ").Replace(")", " ) ")
                        .Replace(": (", ":("); //fix mezera za :
                    string[] mezery = tPart.Split(new char[] { ' ' });

                    foreach (var mt in mezery)
                    {
                        var match = FindPrefixRegex().Match(mt);
                        string prefix = match.Success ? match.Groups["p"].Value : string.Empty;

                        if (!string.IsNullOrEmpty(prefix))
                            tmpParts.Add(new Part()
                                {
                                    ExactValue = false,
                                    Prefix = prefix,
                                    Value = mt.Replace(prefix, "")
                                }
                            );
                        else
                            tmpParts.Add(new Part()
                                {
                                    ExactValue = false,
                                    Value = mt
                                }
                            );
                    }
                }
            }
            //check prefix with xxx:[ ... ]  or xxx:{   }

            List<Part> parts = new List<Part>();

            for (int pi = 0; pi < tmpParts.Count; pi++)
            {
                var p = tmpParts[pi];

                // Skip empty parts during construction
                if (p.ToQueryString.Length == 0)
                    continue;

                if (p.ExactValue)
                {
                    parts.Add(p);
                }
                else if (!string.IsNullOrEmpty(p.Prefix) && (p.Value.StartsWith("{") || p.Value.StartsWith("[")))
                {
                    // Looking for matching closing bracket
                    int endIndex = FindClosingBracketIndex(tmpParts, pi);

                    if (endIndex > pi)
                    {
                        // Found closing bracket - join parts from pi to endIndex
                        var joinedValue = string.Join(" ",
                            tmpParts.Skip(pi).Take(endIndex - pi + 1).Select(m => m.Value));
                        var newPart = new Part()
                        {
                            Prefix = p.Prefix,
                            ExactValue = p.ExactValue,
                            Value = joinedValue
                        };

                        if (newPart.ToQueryString.Length > 0)
                            parts.Add(newPart);

                        pi = endIndex; // Skip processed parts
                    }
                    else
                    {
                        // No closing bracket found - join to end
                        var joinedValue = string.Join(" ", tmpParts.Skip(pi).Select(m => m.Value));
                        var newPart = new Part()
                        {
                            Prefix = p.Prefix,
                            ExactValue = p.ExactValue,
                            Value = joinedValue
                        };

                        if (newPart.ToQueryString.Length > 0)
                            parts.Add(newPart);

                        break;
                    }
                }
                else
                {
                    parts.Add(p);
                }
            }

            return parts.ToArray();
        }

        // Helper method to extract the bracket-finding logic
        // looking until end to the next with ] }
        private int FindClosingBracketIndex(List<Part> tmpParts, int startIndex)
        {
            for (int pj = startIndex + 1; pj < tmpParts.Count; pj++)
            {
                if (tmpParts[startIndex].ExactValue == false
                    && (tmpParts[pj].Value.EndsWith("}") || tmpParts[pj].Value.EndsWith("]")))
                {
                    return pj;
                }
            }

            return -1; // Not found
        }
    }
}