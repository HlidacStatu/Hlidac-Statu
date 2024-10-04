namespace HlidacStatu.Searching
{
    public interface IRule
    {
        RuleResult Process(SplittingQuery.Part queryPart);
        string[] Prefixes { get; }
        string ReplaceWith { get; set; }
        NextStepEnum NextStep { get; set; }
        string AddLastCondition { get; set; }

    }
}
