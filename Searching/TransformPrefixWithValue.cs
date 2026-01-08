using System.Text.RegularExpressions;

namespace HlidacStatu.Searching
{
    public class TransformPrefixWithValue
        : RuleBase
    {

        string _valueConstrain = "";
        string _prefix = "";
        public TransformPrefixWithValue(string prefix, string replaceWith, string valueConstrain, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(replaceWith, stopFurtherProcessing, addLastCondition)
        {
            _valueConstrain = valueConstrain;
            _prefix = prefix;
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
            if (part == null)
                return null;

            if (string.IsNullOrWhiteSpace(ReplaceWith))
                return null; // new RuleResult(SplittingQuery.SplitQuery(" "), this.NextStep);

            if (//this.ReplaceWith.Contains("${q}") &&
                part.Prefix.Equals(_prefix, StringComparison.InvariantCultureIgnoreCase)
                && (
                    string.IsNullOrWhiteSpace(_valueConstrain)
                    || Regex.IsMatch(part.Value, _valueConstrain, Util.Consts.DefaultRegexQueryOption)
                    )
                )
            {
                string rq = " " + ReplaceWith.Replace("${q}", part.Value);
                return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {rq} "), NextStep));
            }

            return null;
        }

    }
}
