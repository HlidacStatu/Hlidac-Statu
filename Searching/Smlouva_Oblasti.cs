using HlidacStatu.Entities;

namespace HlidacStatu.Searching
{
    public class Smlouva_Oblasti
        : RuleBase
    {
        public Smlouva_Oblasti(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        { }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "oblasti:" };
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

            if (part.Prefix.Equals("oblasti:", StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                foreach (var key in AllValues.Keys)
                {
                    if (oblastVal.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var q_obl = $" ( classification.class1.typeValue:${AllValues[key]} OR classification.class2.typeValue:${AllValues[key]} OR classification.class3.typeValue:${AllValues[key]} ) ";

                        return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {q_obl} "), NextStep));
                    }
                }
            }


            return Task.FromResult<RuleResult>(null);
        }

    }
}
