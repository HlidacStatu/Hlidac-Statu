namespace HlidacStatu.Searching
{
    public class VZ_Oblast
        : RuleBase
    {
        private readonly Func<string, string[]> cpvOblastToCpvFunc;

        public VZ_Oblast(Func<string,string[]> cpvOblastToCpvFunc,  bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
            this.cpvOblastToCpvFunc = cpvOblastToCpvFunc;
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "oblast:" };
            }
        }


        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return Task.FromResult<RuleResult>(null);

            if (part.Prefix.Equals("oblast:", StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                var cpvs = cpvOblastToCpvFunc(oblastVal);
                if (cpvs?.Length > 0)
                {
                    string stringCpvs = String.Join(" OR ", cpvs.Select(s => "cPV:" + s + "*"));
                    var q_cpv = $" ( {stringCpvs} ) ";
                    return Task.FromResult( new RuleResult(SplittingQuery.SplitQuery($" {q_cpv} "), NextStep));
                }
            }


            return Task.FromResult<RuleResult>(null);
        }

    }
}
