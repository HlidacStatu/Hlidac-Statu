namespace HlidacStatu.Analysis.Page.Area
{
    public class FoundBoxes
    {
        public System.Drawing.Size ImageSize { get; set; }
        public IEnumerable<System.Drawing.Rectangle> Boundaries { get; set; }
        public System.Drawing.Point RatioFromOriginal { get; set; }

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

        public FoundBoxes GetForAnotherResolution(System.Drawing.Size forAnotherResolution)
        {
            float rateWidth = forAnotherResolution.Width / ImageSize.Width;
            float rateHeight = forAnotherResolution.Height / ImageSize.Height;
            if (Boundaries == null)
                throw new NullReferenceException("Boundaries = null");

            FoundBoxes nfb = new FoundBoxes();
            nfb.ImageSize = forAnotherResolution;
            nfb.Boundaries = Boundaries
                .Select(m => new System.Drawing.Rectangle(
                    (int)(m.X * rateWidth),
                    (int)(m.Y * rateHeight),
                    (int)(m.Width * rateWidth),
                    (int)(m.Height * rateHeight))
                );

            return nfb;
        }

    }

}
