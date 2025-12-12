using Devmasters.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities.Facts
{
    public static class Extensions
    {
        public static string RenderFacts(this IEnumerable<Fact> infofacts, int number,
    bool takeSummary = true, bool shuffle = false,
    string delimiterBetween = " ",
    string lineFormat = "{0}", bool html = false)
        {
            return RenderFacts(infofacts.ToArray(), number, takeSummary, shuffle, delimiterBetween, lineFormat, html);
        }


        public static string RenderFacts(Fact[] Facts, int number,
    bool takeSummary = true, bool shuffle = false,
    string delimiterBetween = " ",
    string lineFormat = "{0}", bool html = false)
        {
            if ((Facts?.Count() ?? 0) == 0)
                return string.Empty;

            IEnumerable<Fact> infof = Facts
                .Where(m => m.Level != Fact.ImportanceLevel.Summary)
                .OrderByDescending(o => o.Level).ToArray();
            Fact summaryIf = Facts.FirstOrDefault(m => m.Level == Fact.ImportanceLevel.Summary);

            var taken = new List<Fact>();

            if (takeSummary && summaryIf != null)
                taken.Add(summaryIf);


            if (taken.Count < number)
            {
                if (shuffle && taken.Count() > 1)
                    taken.AddRange(infof.ShuffleMe().Take(number - taken.Count));
                else
                    taken.AddRange(infof.Take(number - taken.Count));
            }

            bool useStringF = lineFormat.Contains("{0}");

            if (taken.Count == 0)
                return "";
            else
                return String.Join(delimiterBetween, taken
                        .Select(t => useStringF ? string.Format(lineFormat, t.Render(html)) : t.Render(html)));
                    
        }

    }
}
