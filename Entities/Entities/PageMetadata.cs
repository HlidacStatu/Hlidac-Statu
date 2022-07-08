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

        public BlurredMetadata Blurred { get; set; }

        public class BlurredMetadata
        {
            public class Boundary
            {
                public Boundary() { }
                public Boundary(int x, int y, int width, int height) 
                {
                    this.X = x;
                    this.Y = y;
                    this.Width = width;
                    this.Height = height;
                }

                public int X { get; set; }
                public int Y { get; set; }
                public int Width { get; set; }
                public int Height { get; set; }

            }

            public int ImageWidth { get; set; }
            public int ImageHeight { get; set; }

            public long TextArea { get; set; }
            public long BlackenArea { get; set; }

            [Nest.Date]
            public DateTime Created { get; set; }

            [Nest.Keyword()]
            public string AnalyzerVersion { get; set; }

            public Boundary[] TextAreaBoundaries { get; set; }

            public Boundary[] BlackenAreaBoundaries { get; set; }

            public decimal BlackenAreaRatio()
            {
                return (decimal)BlackenArea / (decimal)(BlackenArea + TextArea);
            }

            public BlurredMetadata GetForAnotherResolution(int anotherWidth, int anotherHeight)
            {

                float rateWidth = anotherWidth / ImageWidth;
                float rateHeight = anotherHeight / ImageHeight;

                BlurredMetadata nbm = new BlurredMetadata();
                nbm.Created = this.Created;
                nbm.AnalyzerVersion = this.AnalyzerVersion;
                nbm.ImageWidth = anotherWidth;
                nbm.ImageHeight = anotherHeight;
                if (TextAreaBoundaries != null)
                {
                    nbm.TextAreaBoundaries = TextAreaBoundaries
                        .Select(m => new Boundary(
                            (int)(m.X * rateWidth),
                            (int)(m.Y * rateHeight),
                            (int)(m.Width * rateWidth),
                            (int)(m.Height * rateHeight))
                        ).ToArray();
                    nbm.TextArea = GetTotalArea(nbm.TextAreaBoundaries);
                }
                if (BlackenAreaBoundaries != null)
                {
                    nbm.BlackenAreaBoundaries = BlackenAreaBoundaries
                        .Select(m => new Boundary(
                            (int)(m.X * rateWidth),
                            (int)(m.Y * rateHeight),
                            (int)(m.Width * rateWidth),
                            (int)(m.Height * rateHeight))
                        ).ToArray();
                    nbm.BlackenArea = GetTotalArea(nbm.BlackenAreaBoundaries);
                }
                return nbm;
            }


            public long GetTotalArea(IEnumerable<Boundary> boundaries)
            {
                long area = 0;
                if (boundaries == null)
                    return area;
                foreach (var b in boundaries)
                {
                    area += (long)(b.Width * b.Height);
                }

                return area;
            }

        }
    }
}
