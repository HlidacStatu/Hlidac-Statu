using HlidacStatu.Entities;

namespace HlidacStatu.Searching
{
    public class Dotace_Typ
        : RuleBase
    {
        public Dotace_Typ(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { $"typ:" };
            }
        }

        private static Dictionary<string, string> GetValues()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();



            foreach (var v in Devmasters.Enums.EnumTools.EnumToEnumerable<Dotace.Hint.Type>())
            {       
                ret.Add(v.Value.ToString().ToLower(), ((int)v.Value).ToString());
            }

            return ret;
        }

        public readonly static Dictionary<string, string> AllValues = GetValues();

        protected override Task<RuleResult> processQueryPartAsync(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals(Prefixes.First(), StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                foreach (var key in AllValues.Keys)
                {
                    if (oblastVal.Equals(key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        var q_obl = $"hints.subsidyType:" + AllValues[key.ToLower()];
                        return Task.FromResult(new RuleResult(SplittingQuery.SplitQuery($" {q_obl} "), NextStep));
                    }
                }
            }


            return null;
        }

    }
}
