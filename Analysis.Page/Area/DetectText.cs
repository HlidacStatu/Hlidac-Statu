using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace HlidacStatu.Analysis.Page.Area
{
    public partial class DetectText : IOnPageDetector
    {

        static DetectText()
        {
            string modelPath = Path.Combine(root, "frozen_east_text_detection.pb");
            SharedModel = CvDnn.ReadNet(modelPath);
        }

        public static Net SharedModel = null;

        public static Net NewModel(string modelPath = null)
        {
            modelPath = modelPath ?? Path.Combine(root, "frozen_east_text_detection.pb");
            Net newModel = CvDnn.ReadNet(modelPath);
            return newModel;
        }


        public static readonly string root = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;

        Net net = null;
        private readonly string imageFileName;
        private readonly float confThreshold;
        private readonly float nmsThreshold;
        private readonly int maxSize;
        string tmpFn = "";
        private bool disposedValue;

        private InternalResult internalResult { get; set; } = null;

        public DetectText(Net model, string imageFileName,
            int maxSize = 896, float confThreshold = 0.5f, float nmsThreshold = 0.4f)
        {
            net = model;
            this.imageFileName = imageFileName;
            this.confThreshold = confThreshold;
            this.nmsThreshold = nmsThreshold;
            this.maxSize = NearestMultiple(maxSize, 32); //musi byt nasobek 32

        }

        private int NearestMultiple(int value, int factor)
        {
            int newVal = (int)Math.Round(
                     value / (double)factor,
                     MidpointRounding.AwayFromZero
                 ) * factor;
            return newVal;
        }

        private OpenCvSharp.Size NearestSize(int width, int height)
        {

            int factor = 32;
            if (width > height)
            {
                int nearestW = (int)Math.Round(
                         width / (double)factor,
                         MidpointRounding.AwayFromZero
                     ) * factor;
                if (nearestW > maxSize)
                    nearestW = maxSize;
                float ratio = (float)nearestW / width;

                int nearestH = (int)Math.Round(
                         height * ratio / (double)factor,
                         MidpointRounding.AwayFromZero
                     ) * factor;

                return new Size(nearestW, nearestH);
            }
            else
            {
                int nearestH = (int)Math.Round(
                         height / (double)factor,
                         MidpointRounding.AwayFromZero
                     ) * factor;
                if (nearestH > maxSize)
                    nearestH = maxSize;
                float ratio = (float)nearestH / height;

                int nearestW = (int)Math.Round(
                         width * ratio / (double)factor,
                         MidpointRounding.AwayFromZero
                     ) * factor;

                return new Size(nearestW, nearestH);
            }
        }

        /// <summary>
        /// Read text from image.
        /// </summary>
        /// <see cref="https://github.com/opencv/opencv/blob/master/samples/dnn/text_detection.cpp"/>
        /// <param name="imageFilename">Name of the image file.</param>
        /// <param name="loaderFactory">The loader factory.</param>
        /// <returns>Scanned text.</returns>
        public void AnalyzeImage()
        {
            try
            {
                using (Devmasters.Imaging.InMemoryImage orig = new Devmasters.Imaging.InMemoryImage(imageFileName))
                {
                    OpenCvSharp.Size newSize = NearestSize(orig.Image.Width, orig.Image.Height);
                    orig.Resize(new System.Drawing.Size(newSize.Width, newSize.Height), false, Devmasters.Imaging.InMemoryImage.InterpolationsQuality.High);
                    tmpFn = Path.GetTempFileName();
                    if (orig.Image.Width > orig.Image.Height)
                    {
                        orig.Canvas(new System.Drawing.Size(orig.Image.Width, orig.Image.Width), System.Drawing.Color.White);
                    }
                    orig.SaveAsJPEG(tmpFn, 90);
                }
                internalResult = AnalyzeImg(tmpFn, confThreshold, nmsThreshold);

            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                File.Delete(tmpFn);

            }
        }

        //https://github.com/shimat/opencvsharp/blob/master/test/OpenCvSharp.Tests/dnn/EastTextDetectionTest.cs
        private InternalResult AnalyzeImg(string fileName, float ConfThreshold, float NmsThreshold)
        {
            // Load network.
            using (Mat img = new Mat(fileName))
            {
                var imgsize = new Size(img.Width, img.Height);

                // Prepare input image
                using (var blob = CvDnn.BlobFromImage(img, size: imgsize,
                    mean: new Scalar(123.68, 116.78, 103.94), swapRB: true, crop: false))
                {
                    // Forward Pass
                    // Now that we have prepared the input, we will pass it through the network. There are two outputs of the network.
                    // One specifies the geometry of the Text-box and the other specifies the confidence score of the detected box.
                    // These are given by the layers :
                    //   feature_fusion/concat_3
                    //   feature_fusion/Conv_7/Sigmoid
                    var outputBlobNames = new string[] { "feature_fusion/Conv_7/Sigmoid", "feature_fusion/concat_3" };
                    var outputBlobs = outputBlobNames.Select(_ => new Mat()).ToArray();

                    net.SetInput(blob);
                    net.Forward(outputBlobs, outputBlobNames);
                    Mat scores = outputBlobs[0];
                    Mat geometry = outputBlobs[1];

                    // Decode predicted bounding boxes (decode the positions of the text boxes along with their orientation)
                    Decode(scores, geometry, ConfThreshold, out var boxes, out var confidences);

                    // Apply non-maximum suppression procedure for filtering out the false positives and get the final predictions
                    CvDnn.NMSBoxes(boxes, confidences, ConfThreshold, NmsThreshold, out var indices);

                    res = null;
                    InternalResult ret = new InternalResult();
                    ret.RatioFromOriginal = new Point2f((float)img.Cols / imgsize.Width, (float)img.Rows / imgsize.Height);

                    img.CopyTo(ret.Image);
                    ret.Boundaries = indices.Select(i => boxes[i]);
                    blob.Release();
                    img.Release();
                    return ret;
                }
            }
        }

        public void RenderBoundariesToImage(string modifiedImageFilename = null)
        {
            if (internalResult == null)
                internalResult = AnalyzeImg(tmpFn, confThreshold, nmsThreshold);

            InternalResult indices = internalResult;
            // Optional - Save detections

            //// Render detections.
            foreach (var box in indices.Boundaries)
            {
                Point2f[] vertices = box.Points();
                for (int j = 0; j < 4; ++j)
                {
                    vertices[j].X *= indices.RatioFromOriginal.X;
                    vertices[j].Y *= indices.RatioFromOriginal.Y;
                }

                for (int j = 0; j < 4; ++j)
                {
                    Cv2.Line(indices.Image, (int)vertices[j].X, (int)vertices[j].Y, (int)vertices[(j + 1) % 4].X, (int)vertices[(j + 1) % 4].Y, new Scalar(0, 255, 0), 3);
                }
            }
            modifiedImageFilename = modifiedImageFilename ?? Path.Combine(Path.GetDirectoryName(imageFileName), $"{Path.GetFileNameWithoutExtension(imageFileName)}.texts.jpg");
            indices.Image.SaveImage(modifiedImageFilename);

        }

        private unsafe void Decode(Mat scores, Mat geometry, float confThreshold, out IList<RotatedRect> boxes, out IList<float> confidences)
        {
            boxes = new List<RotatedRect>();
            confidences = new List<float>();

            if (scores == null || scores.Dims != 4 || scores.Size(0) != 1 || scores.Size(1) != 1 ||
                geometry == null || geometry.Dims != 4 || geometry.Size(0) != 1 || geometry.Size(1) != 5 ||
                scores.Size(2) != geometry.Size(2) || scores.Size(3) != geometry.Size(3))
            {
                return;
            }

            int height = scores.Size(2);
            int width = scores.Size(3);

            for (int y = 0; y < height; ++y)
            {
                var scoresData = new ReadOnlySpan<float>((void*)scores.Ptr(0, 0, y), height);
                var x0Data = new ReadOnlySpan<float>((void*)geometry.Ptr(0, 0, y), height);
                var x1Data = new ReadOnlySpan<float>((void*)geometry.Ptr(0, 1, y), height);
                var x2Data = new ReadOnlySpan<float>((void*)geometry.Ptr(0, 2, y), height);
                var x3Data = new ReadOnlySpan<float>((void*)geometry.Ptr(0, 3, y), height);
                var anglesData = new ReadOnlySpan<float>((void*)geometry.Ptr(0, 4, y), height);

                for (int x = 0; x < width; ++x)
                {
                    var score = scoresData[x];
                    if (score >= confThreshold)
                    {
                        float offsetX = x * 4.0f;
                        float offsetY = y * 4.0f;
                        float angle = anglesData[x];
                        float cosA = (float)Math.Cos(angle);
                        float sinA = (float)Math.Sin(angle);
                        float x0 = x0Data[x];
                        float x1 = x1Data[x];
                        float x2 = x2Data[x];
                        float x3 = x3Data[x];
                        float h = x0 + x2;
                        float w = x1 + x3;
                        Point2f offset = new Point2f(offsetX + cosA * x1 + sinA * x2, offsetY - sinA * x1 + cosA * x2);
                        Point2f p1 = new Point2f(-sinA * h + offset.X, -cosA * h + offset.Y);
                        Point2f p3 = new Point2f(-cosA * w + offset.X, sinA * w + offset.Y);
                        RotatedRect r = new RotatedRect(new Point2f(0.5f * (p1.X + p3.X), 0.5f * (p1.Y + p3.Y)), new Size2f(w, h), (float)(-angle * 180.0f / Math.PI));
                        boxes.Add(r);
                        confidences.Add(score);
                    }
                }
            }
        }


        private FoundBoxes res = null;
        public FoundBoxes Result()
        {
            if (disposedValue == false)
            {
                if (res == null)
                {
                    res = new FoundBoxes();
                    res.ImageSize = new System.Drawing.Size(internalResult.Image.Width, internalResult.Image.Height);
                    res.Boundaries = internalResult.Boundaries
                                    .Select(b => new System.Drawing.Rectangle(
                                        b.BoundingRect().X,
                                        b.BoundingRect().Y,
                                        b.BoundingRect().Width,
                                        b.BoundingRect().Height
                                        ));
                    res.RatioFromOriginal = new System.Drawing.Point((int)internalResult.RatioFromOriginal.X, (int)internalResult.RatioFromOriginal.Y);
                }
                return res;
            }
            else
                return null;
        }
        public void ReleaseResources()
        {
            Dispose(disposing: true);
        }


        private class InternalResult
        {
            public Mat Image { get; set; } = new Mat();
            public IEnumerable<RotatedRect> Boundaries { get; set; }

            public Point2f RatioFromOriginal { get; set; }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (internalResult != null)
                    {
                        if (internalResult.Image != null)
                        {
                            internalResult.Image.Dispose();
                        }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DetectTextBoundaries()
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
