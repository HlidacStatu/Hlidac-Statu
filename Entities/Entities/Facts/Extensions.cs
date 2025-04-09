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
            return Fact.RenderFacts(infofacts.ToArray(), number, takeSummary, shuffle, delimiterBetween, lineFormat, html);
        }

    }
}
