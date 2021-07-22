using System.Linq;

namespace HlidacStatu.Repositories.Searching.Rules
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

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (!string.IsNullOrEmpty(part.Prefix))
            {
                return new RuleResult(SplittingQuery.SplitQuery($""), NextStep);
            }
            else
            {
                if (
                    part.ExactValue == false
                    && Tools.DefaultQueryOperators.Contains(part.Value.Trim().ToUpper())
                    )
                {
                    return new RuleResult(SplittingQuery.SplitQuery($""), NextStep);
                }
            }


            return null;
        }

    }
}
