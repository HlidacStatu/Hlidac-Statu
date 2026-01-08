using System.Text.RegularExpressions;

namespace HlidacStatu.Searching
{
    public class TransformPrefixRegex
        : RuleBase
    {
        
        private string _prefix = "";
        private string _regex = "";
        public TransformPrefixRegex(string prefix, string replaceWith, string regex, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(replaceWith, stopFurtherProcessing, addLastCondition)
        {
            _prefix = prefix;
            _regex = regex;
        }


        public override string[] Prefixes
        {
            get
            {
                return new string[] { _prefix };
            }
        }

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null 
                || string.IsNullOrWhiteSpace(ReplaceWith)
                || string.IsNullOrWhiteSpace(_regex)
                || !part.Prefix.Equals(_prefix, StringComparison.InvariantCultureIgnoreCase)
                )
                return null;

            var reres = Regex.Match(part.Value, _regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
            if (reres.Success)
            {
                string rq = " " + ReplaceWith.Replace("${q}", reres.Value);
                return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {rq} "), NextStep));
                
            }

            return null;
        }

    }
}
