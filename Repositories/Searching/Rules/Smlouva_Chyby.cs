using System;
using HlidacStatu.Entities.Issues;

namespace HlidacStatu.Repositories.Searching.Rules
{
    public class Smlouva_Chyby
        : RuleBase
    {
        public Smlouva_Chyby(bool stopFurtherProcessing = false, string addLastCondition = "")
            : base("", stopFurtherProcessing, addLastCondition)
        { }

        public override string[] Prefixes
        {
            get
            {
                return new string[] { "chyby:"};
            }
        }

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (part.Prefix.Equals("chyby:", StringComparison.InvariantCultureIgnoreCase))
            {
                string levelVal = part.Value;
                string levelQ = "";
                if (levelVal == "fatal" || levelVal == "zasadni")
                    levelQ = Entities.Issues.Util.IssuesByLevelQuery(ImportanceLevel.Fatal);
                else if (levelVal == "major" || levelVal == "vazne")
                    levelQ = Entities.Issues.Util.IssuesByLevelQuery(ImportanceLevel.Major);


                return new RuleResult(SplittingQuery.SplitQuery($" {levelQ} "), NextStep);
            }
            return null;//new RuleResult(part, this.NextStep);
        }

    }
}
