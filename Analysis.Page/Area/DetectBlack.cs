using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Drawing;
using System.Drawing.Imaging;

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


            result = new FoundBoxes();
            result.Boundaries = boxes;
            result.ImageSize = image.Size;
            result.RatioFromOriginal = new System.Drawing.Point(1, 1);

        }

        public void RenderBoundariesToImage(string modifiedImageFilename = null)
        {
            if (result == null)
                AnalyzeImage();


            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(image);

            foreach (var blob in result.Boundaries)
            {
                var bl = new System.Drawing.Rectangle((int)blob.X, (int)blob.Y, (int)blob.Width, (int)blob.Height);
                g.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Red, 5.0f), bl);

            }

            modifiedImageFilename = modifiedImageFilename ?? Path.Combine(Path.GetDirectoryName(imagePath), $"{Path.GetFileNameWithoutExtension(imagePath)}.black.jpg");
            image.Save(modifiedImageFilename);
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
