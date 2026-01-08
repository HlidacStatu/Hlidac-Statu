namespace HlidacStatu.Searching
{
    public class VZ_CPV
        : RuleBase
    {
        public VZ_CPV(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        { }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "cpv:" };
            }
        }

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals("cpv:", StringComparison.InvariantCultureIgnoreCase))
            {

                string cpv = "";
                //Match m = Regex.Match(modifiedQ, lookFor, regexQueryOption);
                //string cpv = "";
                //if (m.Success)
                //    cpv = m.Groups["q"].Value;
                cpv = part.Value;
                if (!string.IsNullOrEmpty(cpv))
                {
                    string[] cpvs = cpv.Split(new char[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                    string q_cpv = "";

                    if (cpvs.Length == 0)
                    {
                        return null;
                    }
                    if (cpvs.Length == 1)
                    {
                        if (cpvs[0].EndsWith("*"))
                            return null;
                        else
                            q_cpv = " cPV:" + cpvs[0] + "* ";
                    }
                    else if (cpvs.Length > 1)
                        q_cpv =  $" ( {string.Join(" OR ", cpvs.Select(s => "cPV:" + s + "*"))} ) ";

                    return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {q_cpv} "), NextStep));
                }
            }


            return null;// new RuleResult(part, this.NextStep);
        }

    }
}
