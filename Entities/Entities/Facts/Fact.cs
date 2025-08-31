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
            Salary = 75,
            Stat = 70,
            High = 50,
            Medium = 25,
            Low = 10,
            NotAtAll = 1
        }

        public virtual string Render(bool html = true)
        {
            if (html)
                return Text;
            else
                return Devmasters.TextUtil.RemoveHTML(Text);
        }


    }
}
