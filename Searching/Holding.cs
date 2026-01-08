namespace HlidacStatu.Searching
{
    public class Holding
        : RuleBase
    {
        private readonly Func<string, string[]> icosInHoldingFunc;
        string _specificPrefix = null;
        public Holding(Func<string, string[]> icosInHoldingFunc, string specificPrefix, string replaceWith, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(replaceWith, stopFurtherProcessing, addLastCondition)
        {
            this.icosInHoldingFunc = icosInHoldingFunc;
            _specificPrefix = specificPrefix;
        }

        public override string[] Prefixes
        {
            get
            {
                if (!string.IsNullOrEmpty(_specificPrefix))
                    return new string[] { _specificPrefix };
                else
                    return new string[] { "holding:",
                        "holdingprijemce:", "holdingplatce:",
                        "holdingdluznik:", "holdingveritel:", "holdingspravce:",
                        "holdingdodavatel:", "holdingzadavatel:"};
            }
        }

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (
                (!string.IsNullOrWhiteSpace(_specificPrefix) && part.Prefix.Equals(_specificPrefix, StringComparison.InvariantCultureIgnoreCase))
                ||
                (string.IsNullOrWhiteSpace(_specificPrefix) &&
                (
                    (part.Prefix.Equals("holding:", StringComparison.InvariantCultureIgnoreCase)
                    //RS
                    || part.Prefix.Equals("holdingprijemce:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingplatce:", StringComparison.InvariantCultureIgnoreCase)
                    //insolvence
                    || part.Prefix.Equals("holdingdluznik:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingveritel:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingspravce:", StringComparison.InvariantCultureIgnoreCase)
                    //VZ
                    || part.Prefix.Equals("holdingdodavatel:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingzadavatel:", StringComparison.InvariantCultureIgnoreCase)
                    )
                )
                )
            )
            {
                //list of ICO connected to this holding
                string holdingIco = part.Value;
                string[] holdingIcos = icosInHoldingFunc(holdingIco);

                string icosQuery = "";

                var templ = $" ( {ReplaceWith}{{0}} ) ";
                if (ReplaceWith.Contains("${q}"))
                    templ = $" ( {ReplaceWith.Replace("${q}", "{0}")} )";

                if (holdingIcos != null && holdingIcos.Count() > 0)
                {
                    icosQuery = " ( " + holdingIcos
                        .Select(t => string.Format(templ, t))
                        .Aggregate((fi, s) => fi + " OR " + s) + " ) ";
                }
                else
                {
                    icosQuery = string.Format(templ, "noOne"); //$" ( {icoprefix}:noOne ) ";
                }

                return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($"{icosQuery}"), NextStep));

            }

            return null;
        }

    }
}
