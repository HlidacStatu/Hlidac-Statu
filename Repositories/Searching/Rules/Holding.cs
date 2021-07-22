using System;
using System.Linq;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories.Searching.Rules
{
    public class Holding
        : RuleBase
    {
        string _specificPrefix = null;
        public Holding(string specificPrefix, string replaceWith, bool stopFurtherProcessing = false, string addLastCondition = "")
            : base(replaceWith, stopFurtherProcessing, addLastCondition)
        {
            _specificPrefix = specificPrefix;
        }

        public override string[] Prefixes { 
            get {
                if (!string.IsNullOrEmpty(_specificPrefix))
                    return new string[] { _specificPrefix };
                else
                    return new string[] { "holding:", 
                        "holdingprijemce:", "holdingplatce:",
                        "holdingdluznik:", "holdingveritel:", "holdingspravce:",
                        "holdingdodavatel:", "holdingzadavatel:"};
            }
        }

        protected override RuleResult processQueryPart(SplittingQuery.Part part)
        {
            if (part == null)
                return null;

            if (
                (!string.IsNullOrWhiteSpace(_specificPrefix) && part.Prefix.Equals(_specificPrefix, StringComparison.InvariantCultureIgnoreCase))
                ||
                (string.IsNullOrWhiteSpace(_specificPrefix) &&
                (
                    (part.Prefix.Equals("holding:", StringComparison.InvariantCultureIgnoreCase)
                    //RS
                    || part.Prefix.Equals("holdingprijemce:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingplatce:", StringComparison.InvariantCultureIgnoreCase)
                    //insolvence
                    || part.Prefix.Equals("holdingdluznik:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingveritel:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingspravce:", StringComparison.InvariantCultureIgnoreCase)
                    //VZ
                    || part.Prefix.Equals("holdingdodavatel:", StringComparison.InvariantCultureIgnoreCase)
                    || part.Prefix.Equals("holdingzadavatel:", StringComparison.InvariantCultureIgnoreCase)
                    )
                )
                )
            )
            {
                //list of ICO connected to this holding
                string holdingIco = part.Value;
                Relation.AktualnostType aktualnost = Relation.AktualnostType.Nedavny;
                Firma f = Firmy.Get(holdingIco);
                if (f != null && f.Valid)
                {
                    var icos = new string[] { f.ICO }
                        .Union(
                            f.AktualniVazby(aktualnost)
                            .Select(s => s.To.Id)
                        )
                        .Distinct();
                    string icosQuery = "";
                    var icosPresLidi = f.AktualniVazby(aktualnost)
                            .Where(o => o.To.Type == Datastructures.Graphs.Graph.Node.NodeType.Person)
                            .Select(o => Osoby.GetById.Get(Convert.ToInt32(o.To.Id)))
                            .Where(o => o != null)
                            .SelectMany(o => o.AktualniVazby(aktualnost))
                            .Select(v => v.To.Id)
                            .Distinct();
                    icos = icos.Union(icosPresLidi).Distinct();

                    var templ = $" ( {ReplaceWith}{{0}} ) ";
                    if (ReplaceWith.Contains("${q}"))
                        templ = $" ( {ReplaceWith.Replace("${q}", "{0}")} )";

                    if (icos != null && icos.Count() > 0)
                    {
                        icosQuery = " ( " + icos
                            .Select(t => string.Format(templ, t))
                            .Aggregate((fi, s) => fi + " OR " + s) + " ) ";
                    }
                    else
                    {
                        icosQuery = string.Format(templ, "noOne"); //$" ( {icoprefix}:noOne ) ";
                    }

                    return new RuleResult(SplittingQuery.SplitQuery($"{icosQuery}"), NextStep);
                }
            }

            return null;
        }

    }
}
