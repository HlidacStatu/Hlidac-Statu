﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Searching
{
    public class Firmy_OVMKategorie
        : RuleBase
    {
        public Firmy_OVMKategorie(Dictionary<int, string[]> validValues, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        {
            this.validValues = validValues;
        }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "kategorieid:", "kategorie:" };
            }
        }

        private readonly Dictionary<int, string[]> validValues;

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals("kategorieid:", StringComparison.InvariantCultureIgnoreCase))
            {
                var katId = part.Value;
                foreach (var key in validValues.Keys)
                {
                    if (katId.Equals(key.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        string icosQuery = " ( " + validValues[key]
                                .Select(t => $"ico:{t}")
                                .Aggregate((f, s) => f + " OR " + s) + " ) ";
                        return new RuleResult(SplittingQuery.SplitQuery($"{icosQuery}"), NextStep);
                    }
                }
            }


            return null;
        }

    }
}
