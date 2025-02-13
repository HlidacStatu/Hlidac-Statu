using HlidacStatu.Entities;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Searching
{
    public class Dotace_Oblast
        : RuleBase
    {
        public Dotace_Oblast(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { $"oblast:" };
            }
        }

        private static Dictionary<string, string> GetOblastiValues()
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();



            foreach (var v in Devmasters.Enums.EnumTools.EnumToEnumerable<Dotace.Hint.CalculatedCategories>())
            {       
                ret.Add(v.Value.ToString().ToLower(), ((int)v.Value).ToString());
            }

            return ret;
        }

        public readonly static Dictionary<string, string> AllValues = GetOblastiValues();

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals(Prefixes.First(), StringComparison.InvariantCultureIgnoreCase))
            {
                var oblastVal = part.Value;
                foreach (var key in AllValues.Keys)
                {
                    if (
                        oblastVal.Equals(key, StringComparison.InvariantCultureIgnoreCase)
                        || oblastVal.Equals(AllValues[key.ToLower()], StringComparison.InvariantCultureIgnoreCase)
                        )
                    {
                        var q_obl = $"hints.category1.typeValue:" + AllValues[key.ToLower()];
                        return new RuleResult(SplittingQuery.SplitQuery($" {q_obl} "), NextStep);
                    }
                }
            }


            return null;
        }

    }
}
