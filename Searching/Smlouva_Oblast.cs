using HlidacStatu.Entities;

namespace HlidacStatu.Searching
{
    public class Smlouva_Oblast
        : RuleBase
    {
        int place = 0;
        public Smlouva_Oblast(int place, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
            this.place = place;
            if (this.place < 1)
                this.place = 1;
            if (this.place > 2)
                this.place = 2;
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { $"oblast{(place == 1 ? "" : "2")}:" };
            }
        }

        private static Dictionary<string, string> GetOblastiValues()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();



            foreach (var v in Smlouva.SClassification.AllTypes)
            {
                if (v.IsMainType)
                {
                    ret.Add(v.SearchShortcut, v.SearchExpression);
                    ret.Add(v.SearchShortcut + "_obecne", v.SearchExpression);
                }
                else
                    ret.Add(v.SearchShortcut, v.SearchExpression);

            }

            return ret;
        }

        public readonly static Dictionary<string, string> AllValues = GetOblastiValues();

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return Task.FromResult<RuleResult>(null);

            if (part.Prefix.Equals(Prefixes.First(), StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                foreach (var key in AllValues.Keys)
                {
                    if (oblastVal.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var q_obl = $"classification.class{place}.typeValue:" + AllValues[key];
                        return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {q_obl} "), NextStep));
                    }
                }
            }


            return Task.FromResult<RuleResult>(null);
        }

    }
}
