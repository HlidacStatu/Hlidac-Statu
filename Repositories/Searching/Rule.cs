using System;

namespace HlidacStatu.Repositories.Searching
{
    [Obsolete]
    public class Rule
    {
        public Rule(string lookFor, string replaceWith, bool fullReplace = true)
        {
            LookFor = lookFor;
            ReplaceWith = replaceWith;
            FullReplace = fullReplace;
        }


        public string LookFor { get; set; }
        public string ReplaceWith { get; set; }

        public bool FullReplace { get; set; } = true;
        public string AddLastCondition { get; set; } = "";
    }
}
