using Devmasters.Collections;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Facts
{
    public abstract class Fact
    {
        public Fact() { }
        public Fact(string txt, ImportanceLevel level)
        {
            Text = txt;
            Level = level;
        }

        public string Text { get; set; }
        public ImportanceLevel Level { get; set; }

        public enum ImportanceLevel
        {
            Summary = 100,
            Stat = 70,
            High = 50,
            Medium = 25,
            Low = 10,
            NotAtAll = 1
        }

        public string Render(bool html = true)
        {
            if (html)
                return Text;
            else
                return Devmasters.TextUtil.RemoveHTML(Text);
        }

        public static string RenderFacts(Fact[] Facts, int number,
            bool takeSummary = true, bool shuffle = false,
            string delimiterBetween = " ",
            string lineFormat = "{0}", bool html = false)
        {
            if ((Facts?.Count() ?? 0) == 0)
                return string.Empty;

            IEnumerable<Fact> infof = Facts
                .Where(m => m.Level != ImportanceLevel.Summary)
                .OrderByDescending(o => o.Level).ToArray();
            Fact summaryIf = Facts.FirstOrDefault(m => m.Level == ImportanceLevel.Summary);

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
                return taken
                    .Select(t => useStringF ? string.Format(lineFormat, t.Render(html)) : t.Render(html))
                    .Aggregate((f, s) => f + delimiterBetween + s);
        }

    }
}
