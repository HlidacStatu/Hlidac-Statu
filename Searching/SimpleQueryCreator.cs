using Nest;

namespace HlidacStatu.Searching
{
    public static class SimpleQueryCreator
    {
        public static async Task<SplittingQuery> GetSimpleQueryAsync(string query, IRule[] rules)
        {
            var fixedQuery = Tools.FixInvalidQuery(query, rules) ?? "";
            var sq = SplittingQuery.SplitQuery(fixedQuery);
            return await GetSimpleQueryAsync(sq, rules);
        }


        public static async Task<SplittingQuery> GetSimpleQueryAsync(SplittingQuery sq, IRule[] rules)
        {
            SplittingQuery finalSq = new SplittingQuery();

            if (rules.Count() == 0)
                finalSq = sq;

            Dictionary<int, RuleResult> queryPartResults = new Dictionary<int, RuleResult>();
            for (int qi = 0; qi < sq.Parts.Length; qi++)
            {
                //beru cast dotazu
                queryPartResults.Add(qi, new RuleResult());

                //aplikuju na nej jednotliva pravidla, nasledujici na vysledek predchoziho
                SplittingQuery.Part[] qToProcess = null;
                List<RuleResult> qpResults = new List<RuleResult>();
                foreach (var rule in rules)
                {
                    qToProcess = qToProcess ?? new SplittingQuery.Part[] { sq.Parts[qi] };
                    qpResults = new List<RuleResult>();
                    foreach (var qp in qToProcess)
                    {
                        var partRest = await rule.ProcessAsync(qp);
                        if (partRest != null)
                            qpResults.Add(partRest);
                        else
                            qpResults.Add(new RuleResult(qp, rule.NextStep));
                    }
                    if (qpResults.Last().NextStep == NextStepEnum.StopFurtherProcessing)
                        break;

                    qToProcess = qpResults
                        .SelectMany(m => m.Query.Parts)
                        .Where(m => m.ToQueryString.Length > 0)
                        .ToArray();

                } //rules

                queryPartResults[qi] = new RuleResult(new SplittingQuery(qToProcess), qpResults.LastOrDefault()?.NextStep ?? NextStepEnum.Finished);

            } //qi all query parts

            foreach (var qp in queryPartResults)
            {
                finalSq.AddParts(qp.Value.Query.Parts);
            }

            return finalSq;
        }


        public static async Task<QueryContainer> GetSimpleQueryAsync<T>(string query, IRule[] rules, string[] fields = null)
            where T : class
        {
            var sq = await GetSimpleQueryAsync(query, rules);
            string modifiedQ = sq.FullQuery();

            QueryContainer qc = null;
            if (modifiedQ == null)
                qc = new QueryContainerDescriptor<T>().MatchNone();
            else if (string.IsNullOrEmpty(modifiedQ) || modifiedQ == "*")
                qc = new QueryContainerDescriptor<T>().MatchAll();
            else
            {
                modifiedQ = modifiedQ.Replace(" | ", " OR ").Trim();
                qc = new QueryContainerDescriptor<T>()
                    .QueryString(qs => qs
                        .Query(modifiedQ)
                        .DefaultOperator(Operator.And)
                        .Fields(fields)
                    );
            }
            return qc;
        }

    }
}