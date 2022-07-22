namespace HlidacStatu.Analysis.Page.Area
{
    public class AnalyzedPdf
    {
        public string uniqueId { get; set; }
        public PageMetadata[] pages { get; set; }
        public class PageMetadata
        {
            public class Boundary
            {
                public Boundary() { }


                public int x { get; set; }
                public int y { get; set; }
                public int width { get; set; }
                public int height { get; set; }


            }

            public int page { get; set; }

            public int imageWidth { get; set; }
            public int imageHeight { get; set; }

            public long textArea { get; set; }
            public long blackenArea { get; set; }

            public DateTime created { get; set; }

            public string analyzerVersion { get; set; }

            public Boundary[] textAreaBoundaries { get; set; }

            public Boundary[] blackenAreaBoundaries { get; set; }


        }


    }
}
