using System;
using System.Linq;

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


        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals("oblast:", StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                var cpvs = cpvOblastToCpvFunc(oblastVal);
                if (cpvs?.Length > 0)
                {
                    //var q_cpv = "cPV:(" + cpvs.Select(s => s + "*").Aggregate((f, s) => f + " OR " + s) + ")";
                    var q_cpv = " ( " + cpvs.Select(s => "cPV:" + s + "*").Aggregate((f, s) => f + " OR " + s) + " ) ";
                    return new RuleResult(SplittingQuery.SplitQuery($" {q_cpv} "), NextStep);
                }
            }


            return null;
        }

    }
}
