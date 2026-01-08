namespace HlidacStatu.Searching
{
    public class RemoveAllOperators
        : RuleBase
    {
        public RemoveAllOperators(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        { }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { };
            }
        }

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (!string.IsNullOrEmpty(part.Prefix))
            {
                return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($""), NextStep));
            }
            else
            {
                if (
                    part.ExactValue == false
                    && Tools.DefaultQueryOperators.Contains(part.Value.Trim().ToUpper())
                    )
                {
                    return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($""), NextStep));
                }
            }


            return null;
        }

    }
}
