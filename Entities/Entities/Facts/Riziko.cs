using Devmasters.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities.Facts
{
    public class Riziko : Fact
    {
        public int Rok { get; set; }
        public string Html { get; set; }
        public Riziko() { }
        public Riziko(string txt, string html, ImportanceLevel level)
            :base(txt, level)
        {
            Html = html;
        }

        public override string Render(bool html = true)
        {
            if (html)
                return Html;
            else
                return Text;
        }
    }
}
