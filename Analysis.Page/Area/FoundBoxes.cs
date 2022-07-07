using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Analysis.Page.Area
{
    public class FoundBoxes
    {
        public System.Drawing.SizeF ImageSize { get; set; }
        public IEnumerable<System.Drawing.RectangleF> Boundaries { get; set; }
        public System.Drawing.PointF RatioFromOriginal { get; set; }

        public long GetTotalArea()
        {
            if (Boundaries == null)
                throw new NullReferenceException("Boundaries = null");
            long area = 0;
            foreach (var b in Boundaries)
            {
                area += (long)(b.Width * b.Height);
            }

            return area;
        }

        public FoundBoxes GetForAnotherResolution(System.Drawing.SizeF forAnotherResolution)
        {
            float rateWidth = forAnotherResolution.Width / ImageSize.Width;
            float rateHeight = forAnotherResolution.Height / ImageSize.Height;
            if (Boundaries == null)
                throw new NullReferenceException("Boundaries = null");

            FoundBoxes nfb = new FoundBoxes();
            nfb.ImageSize = forAnotherResolution;
            nfb.Boundaries = Boundaries
                .Select(m => new System.Drawing.RectangleF(
                    (int)(m.X * rateWidth),
                    (int)(m.Y * rateHeight),
                    (int)(m.Width * rateWidth),
                    (int)(m.Height * rateHeight))
                );

            return nfb;
        }

    }

}
