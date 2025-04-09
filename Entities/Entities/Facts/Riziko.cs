using Devmasters.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Facts
{
    public class Riziko : Fact
    {
        public int Rok { get; set; }
        public Riziko() { }
        public Riziko(string txt, ImportanceLevel level)
        {
            Text = txt;
            Level = level;
        }

    }
}
