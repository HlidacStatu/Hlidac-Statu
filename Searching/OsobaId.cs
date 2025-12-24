using HlidacStatu.DS.Graphs;
using System.Text.RegularExpressions;

namespace HlidacStatu.Searching
{
    public class OsobaId
        : RuleBase
    {
        private readonly Func<string, Relation.CharakterVazbyEnum, string[]> icosConnectedToPerson;
        string _specificPrefix = null;

        public OsobaId(Func<string, Relation.CharakterVazbyEnum, string[]> icosConnectedToPerson, string specificPrefix, string replaceWith,
            bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(replaceWith, stopFurtherProcessing, addLastCondition)
        {
            this.icosConnectedToPerson = icosConnectedToPerson;
            _specificPrefix = specificPrefix;
        }

        public override string[] Prefixes
        {
            get
            {
                if (!string.IsNullOrEmpty(_specificPrefix))
                    return new string[] { _specificPrefix };
                else
                    return new string[]
                    {
                        "osobaid:",
                        "osobaidprijemce:", "osobaidplatce:",
                        "osobaiddluznik:", "osobaidveritel:",
                        "osobaidspravce:", "osobaiddodavatel:", "osobaidzadavatel:"
                    };
            }
        }

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;


            if (
                (
                    (!string.IsNullOrWhiteSpace(_specificPrefix) && part.Prefix.Equals(_specificPrefix,
                        StringComparison.InvariantCultureIgnoreCase))
                    ||
                    (string.IsNullOrWhiteSpace(_specificPrefix) &&
                     (
                         (part.Prefix.Equals("osobaid:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidprijemce:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidplatce:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidveritel:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidveritel:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidspravce:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaidzadavatel:", StringComparison.InvariantCultureIgnoreCase)
                          || part.Prefix.Equals("osobaiddodavatel:", StringComparison.InvariantCultureIgnoreCase)
                         )
                     )
                    )
                )
                && (Regex.IsMatch(part.Value, @"(?<q>((\w{1,} [-]{1} \w{1,})([-]{1} \d{1,3})?))",
                    Util.Consts.DefaultRegexQueryOption))
            )
            {
                if (!string.IsNullOrWhiteSpace(ReplaceWith))
                {
                    //list of ICO connected to this person
                    string nameId = part.Value;
                    string icosQuery = "";

                    var templ = $" ( {ReplaceWith}{{0}} ) ";
                    if (ReplaceWith.Contains("${q}"))
                        templ = $" ( {ReplaceWith.Replace("${q}", "{0}")} )";
                    var icos = this.icosConnectedToPerson(nameId, Relation.CharakterVazbyEnum.VlastnictviKontrola);
                    if (icos != null && icos.Length > 0)
                    {
                        icosQuery = $" ( {string.Join(" OR ", icos
                            .Select(t => string.Format(templ, t)))} ) ";
                    }
                    else
                    {
                        icosQuery = string.Format(templ, "noOne"); //$" ( {icoprefix}:noOne ) ";
                    }

                    bool lastCondAdded = false;
                    if (!string.IsNullOrEmpty(AddLastCondition))
                    {
                        if (AddLastCondition.Contains("${q}"))
                        {
                            icosQuery = Query.ModifyQueryOR(icosQuery, AddLastCondition.Replace("${q}", part.Value));
                        }
                        else
                        {
                            icosQuery = Query.ModifyQueryOR(icosQuery, AddLastCondition);
                        }

                        lastCondAdded = true;
                        //this.AddLastCondition = null; //done, don't do it anywhere
                    }

                    return new RuleResult(SplittingQuery.SplitQuery($"{icosQuery}"), NextStep, lastCondAdded);
                } // if (!string.IsNullOrWhiteSpace(this.ReplaceWith))
                else if (!string.IsNullOrWhiteSpace(AddLastCondition))
                {
                    if (AddLastCondition.Contains("${q}"))
                    {
                        var q = AddLastCondition.Replace("${q}", part.Value);
                        return new RuleResult(SplittingQuery.SplitQuery(q), NextStep, true);
                    }
                    else
                    {
                        var q = AddLastCondition;
                        return new RuleResult(SplittingQuery.SplitQuery(q), NextStep, true);
                    }
                }
            }

            return null;
        }
    }
}