using Devmasters.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Facts
{
    public class InfoFact : Fact
    {
        public InfoFact() { }
        public InfoFact(string txt, ImportanceLevel level)
        {
            Text = txt;
            Level = level;
        }

    }
}
