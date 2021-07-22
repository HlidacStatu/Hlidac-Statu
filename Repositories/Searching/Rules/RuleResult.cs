namespace HlidacStatu.Repositories.Searching.Rules
{
    public class RuleResult
    {
        public RuleResult()
        {
            Query = new SplittingQuery();
            NextStep = NextStepEnum.Process;
        }
        public RuleResult(SplittingQuery sq, NextStepEnum nextStep, bool lastConditionAdded = false)
        {
            Query = sq;
            NextStep = nextStep;
            LastConditionAdded = lastConditionAdded;
        }

        public RuleResult(SplittingQuery.Part queryPart, NextStepEnum nextStep, bool lastConditionAdded = false)
        {
            Query = new SplittingQuery(new[] { queryPart });
            NextStep = nextStep;
            LastConditionAdded = lastConditionAdded;
        }
        public RuleResult(SplittingQuery.Part[] queryParts, NextStepEnum nextStep)
        {
            Query = new SplittingQuery(queryParts);
            NextStep = nextStep;
        }

        public SplittingQuery Query { get; set; }
        public NextStepEnum NextStep { get; set; }
        public bool LastConditionAdded { get; set; } = false;
    }
}
