
using System.Drawing;

using Accord.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace HlidacStatu.Analysis.Page.Area
{
    public class DetectBlack2 : IOnPageDetector
    {
        private readonly string imagePath;
        private bool disposedValue;
        System.Drawing.Bitmap image = null;

        FoundBoxes result = null;
        public decimal MaxNonBlackPixesThreshold { get; set; } = 0.1m;
        public DetectBlack2(string imagePath)
        {
            this.imagePath = imagePath;
        }

        //https://emgu.com/wiki/index.php/Shape_(Triangle,_Rectangle,_Circle,_Line)_Detection_in_CSharp#Source_Code
        public void AnalyzeImage()
        {
            image = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(imagePath);
            Mat img = image.ToMat();

            List<Triangle2DF> triangleList = new List<Triangle2DF>();
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle


            using (UMat gray = new UMat())
            using (UMat cannyEdges = new UMat())
            {
                //Convert the image to grayscale and filter out the noise
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

                //Remove noise
                //CvInvoke.GaussianBlur(gray, gray, new Size(3, 3), 1);

                #region Canny and edge detection
                double cannyThreshold = 180.0;
                double cannyThresholdLinking = 120.0;
                CvInvoke.Canny(gray, cannyEdges, cannyThreshold, cannyThresholdLinking);


                //LineSegment2D[] lines = CvInvoke.HoughLinesP(
                //    cannyEdges,
                //    1, //Distance resolution in pixel-related units
                //    Math.PI / 45.0, //Angle resolution measured in radians.
                //    20, //threshold
                //    30, //min Line width
                //    10); //gap between lines
                #endregion

                #region Find triangles and rectangles
                using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                {
                    CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List,
                        ChainApproxMethod.ChainApproxSimple);
                    int count = contours.Size;
                    for (int i = 0; i < count; i++)
                    {
                        using (VectorOfPoint contour = contours[i])
                        using (VectorOfPoint approxContour = new VectorOfPoint())
                        {
                            CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05,
                                true);
                            if (CvInvoke.ContourArea(approxContour, false) > 250) //only consider contours with area greater than 250
                            {
                                if (approxContour.Size == 3) //The contour has 3 vertices, it is a triangle
                                {
                                    Point[] pts = approxContour.ToArray();
                                    triangleList.Add(new Triangle2DF(
                                        pts[0],
                                        pts[1],
                                        pts[2]
                                    ));
                                }
                                else if (approxContour.Size == 4) //The contour has 4 vertices.
                                {
                                    #region determine if all the angles in the contour are within [80, 100] degree
                                    bool isRectangle = true;
                                    Point[] pts = approxContour.ToArray();
                                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                    for (int j = 0; j < edges.Length; j++)
                                    {
                                        double angle = Math.Abs(
                                            edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                        if (angle < 80 || angle > 100)
                                        {
                                            isRectangle = false;
                                            break;
                                        }
                                    }

                                    #endregion

                                    if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                                }
                            }
                        }
                    }
                }
                #endregion
            }
            var allboxes = boxList.Select(m => m.MinAreaRect());

            List<Rectangle> filledBoxesWithBlack = new List<Rectangle>();
            using (Accord.Imaging.UnmanagedImage ddmp = UnmanagedImage.FromManagedImage(image))
            {

                foreach (var box in allboxes)
                {
                    int maxNonBlackPixes = (int)(box.Width * box.Height * MaxNonBlackPixesThreshold);
                    //invert color
                    Color blackColor = Color.FromArgb(255 - 220,
                        255 - 220, 255 - 220);
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
            result = new FoundBoxes();
            result.Boundaries = filledBoxesWithBlack.ToArray();
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
