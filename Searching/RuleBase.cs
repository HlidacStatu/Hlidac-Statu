namespace HlidacStatu.Searching
{

    public enum NextStepEnum
    {
        Finished,
        Process,
        StopFurtherProcessing
    }

    public abstract class RuleBase
        : IRule
    {
        public RuleBase(string replaceWith, bool stopFurtherProcessing = false, string addLastCondition = "")
        {
            ReplaceWith = replaceWith;
            NextStep = stopFurtherProcessing
                ? NextStepEnum.StopFurtherProcessing : NextStepEnum.Process;
            AddLastCondition = addLastCondition;
        }

        public abstract string[] Prefixes { get; }

        public string ReplaceWith { get; set; }

        public NextStepEnum NextStep { get; set; } = NextStepEnum.Process;
        public string AddLastCondition { get; set; } = "";

        public virtual async Task<RuleResult> ProcessAsync(SplittingQuery.Part queryPart)
        {
            var res = await processQueryPartAsync(queryPart);
            
            if (res != null && res.LastConditionAdded == false && !string.IsNullOrEmpty(AddLastCondition))
            {
                string rq = AddLastCondition;
                if (AddLastCondition.Contains("${q}"))
                {
                    rq = AddLastCondition.Replace("${q}", queryPart.Value);
                }
                if (res != null)
                    rq = Query.ModifyQueryOR(res.Query.FullQuery(), rq);

                return new RuleResult(SplittingQuery.SplitQuery($" {rq} "), NextStep);
            }

            return res;
        }
        protected abstract Task<RuleResult> processQueryPartAsync(SplittingQuery.Part queryPart);
    }
}
