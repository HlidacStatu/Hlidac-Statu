using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Entities.Entities
{
    public class PageMetadata
    {
        [Nest.Keyword()]
        public string SmlouvaId { get; set; }
        [Nest.Keyword()]
        public string PrilohaId { get; set; }

        [Nest.Number]
        public int? PageNum { get; set; }

        public Conceal ConcealMetadata { get; set; }

        public class Conceal
        {
            public int ImageWidth { get; set; }
            public int ImageHeight { get; set; }

            public long TextArea { get; set; }
            public long BlackedArea { get; set; }

            [Nest.Date]
            public DateTime Created { get; set; }

            [Nest.Keyword()]
            public string AnalyzerVersion { get; set; }

            public System.Drawing.RectangleF TextAreaBoundaries { get; set; }

            public System.Drawing.RectangleF BlackedAreaBoundaries { get; set; }

            public decimal BlackedAreaRatio()
            {
                return (decimal)BlackedArea / (decimal)(BlackedArea + TextArea);
            }
        }
    }
}
