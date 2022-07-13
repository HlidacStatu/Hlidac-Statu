
using System.Drawing;
using System.Drawing.Drawing2D;

using Accord;
using Accord.Imaging;
using Accord.Imaging.Filters;
using Accord.Math.Geometry;

namespace HlidacStatu.Analysis.Page.Area
{
    public class DetectBlack : IOnPageDetector
    {
        private readonly string imagePath;
        private bool disposedValue;
        System.Drawing.Bitmap image = null;

        FoundBoxes result = null;
        public decimal MaxNonBlackPixesThreshold { get; set; } = 0.1m;
        public DetectBlack(string imagePath)
        {
            this.imagePath = imagePath;
        }

        public void AnalyzeImage()
        {
            image = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(imagePath);

            var invImage = new Invert().Apply(image);

            var fullArea = image.Width * image.Height;
            int blackArea = 0;

            ColorFiltering colorFilter = new ColorFiltering();

            colorFilter.Red = new IntRange(0, 64);
            colorFilter.Green = new IntRange(0, 64);
            colorFilter.Blue = new IntRange(0, 64);
            colorFilter.FillOutsideRange = false;

            colorFilter.ApplyInPlace(invImage);

            // locating objects
            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 20;
            blobCounter.MinWidth = 40;
            blobCounter.BackgroundThreshold = System.Drawing.Color.FromArgb(220, 220, 220);


            blobCounter.ProcessImage(invImage);

            Blob[] blobs = blobCounter.GetObjectsInformation();



            // check for rectangles

            List<System.Drawing.Rectangle> boxes = new List<System.Drawing.Rectangle>();

            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();
            //shapeChecker.AngleError = 15;
            //shapeChecker.RelativeDistortionLimit = 0.08f;
            for (int i = 0; i < blobs.Length; i++)
            {
                var blob = blobs[i];

                List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blob);
                List<IntPoint> cornerPoints;
                if (edgePoints != null && edgePoints.Count > 1)
                {
                    var shapeType = shapeChecker.CheckShapeType(edgePoints);
                    shapeChecker.IsConvexPolygon(edgePoints, out cornerPoints);
                    var subShapeType = shapeChecker.CheckPolygonSubType(cornerPoints);
                    // use the shape checker to extract the corner points
                    if (
                        shapeType == ShapeType.Triangle && subShapeType == PolygonSubType.RectangledTriangle
                        || shapeType == ShapeType.Triangle && subShapeType == PolygonSubType.RectangledIsoscelesTriangle
                        || shapeType == ShapeType.Quadrilateral && subShapeType == PolygonSubType.Rectangle
                        || shapeType == ShapeType.Quadrilateral && subShapeType == PolygonSubType.Trapezoid
                        )
                    {
                        boxes.Add(blob.Rectangle);
                    }
                }
            }

            List<Rectangle> filledBoxesWithBlack = new List<Rectangle>();


            if (true)
            {
                using (Accord.Imaging.UnmanagedImage ddmp = UnmanagedImage.FromManagedImage(image))
                {

                    foreach (var box in boxes)
                    {
                        int maxNonBlackPixes = (int)(box.Width * box.Height * MaxNonBlackPixesThreshold);
                        //invert color
                        Color blackColor = Color.FromArgb(255 - blobCounter.BackgroundThreshold.R,
                            255 - blobCounter.BackgroundThreshold.G, 255 - blobCounter.BackgroundThreshold.B);
                        int nonBlack = 0;
                        for (int x = 0; x < box.Width; x++)
                        {
                            for (int y = 0; y < box.Height; y++)
                            {
                                var pixCol = ddmp.GetPixel(x + box.X, y + box.Y);
                                if (pixCol.R > blackColor.R
                                    || pixCol.G > blackColor.G
                                    || pixCol.B > blackColor.B
                                    )
                                    nonBlack++;

                                if (nonBlack > maxNonBlackPixes)
                                    goto endF;

                            }

                        }
endF:
                        if (nonBlack <= maxNonBlackPixes)
                            filledBoxesWithBlack.Add(box);
                    }

                }

            }
            else
                filledBoxesWithBlack = boxes.ToList();

            result = new FoundBoxes();
            result.Boundaries = filledBoxesWithBlack;
            result.ImageSize = image.Size;
            result.RatioFromOriginal = new System.Drawing.Point(1, 1);

        }

        public void RenderBoundariesToImage(string modifiedImageFilename = null)
        {
            if (result == null)
                AnalyzeImage();


            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image))
            {
                int num = 0;
                foreach (var blob in result.Boundaries)
                {
                    num++;
                    var bl = new System.Drawing.Rectangle((int)blob.X, (int)blob.Y, (int)blob.Width, (int)blob.Height);
                    g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Red, 5.0f), bl);
                    //g.DrawString(num.ToString(), SystemFonts.DefaultFont, Brushes.Red, new PointF(blob.X, blob.Y));
                }


                modifiedImageFilename = modifiedImageFilename ?? Path.Combine(Path.GetDirectoryName(imagePath), $"{Path.GetFileNameWithoutExtension(imagePath)}.black.jpg");
                image.Save(modifiedImageFilename, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        public FoundBoxes Result()
        {
            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (image != null)
                        image.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DetectBlack()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
