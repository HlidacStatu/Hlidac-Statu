using Devmasters.Imaging;

using Emgu.CV;
using Emgu.CV.Structure;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace HlidacStatu.DetectFaces
{
    public class FromImage
    {
        static string modelFilename = "haarcascade_frontalface_default.xml";
        static FromImage()
        {
            if (!System.IO.File.Exists(modelFilename))
            {
                try
                {
                    System.IO.File.WriteAllBytes(modelFilename,
                        new System.Net.Http.HttpClient().GetByteArrayAsync("https://somedata.hlidacstatu.cz/AppData/haarcascade_frontalface_default.xml").Result
                    );
                }
                catch (Exception)
                {

                }
            }
        }
        public static IEnumerable<string> DetectAndParseFacesIntoFiles(byte[] imageData, int minSize, int marginInPercent)
        {
            List<string> files = new List<string>();
            var fnroot = System.IO.Path.GetTempFileName();
            int count = 0;
            foreach (var img in DetectAndParseFaces(imageData, minSize, marginInPercent))
            {
                var fn = fnroot + "." + count + ".faces.jpg";
                System.IO.File.WriteAllBytes(fn, img);
                System.Threading.Thread.Sleep(50);
                files.Add(fn);
                count++;
            }
            return files;
        }

        public static IEnumerable<byte[]> DetectAndParseFaces(Byte[] imagedata, int minSize, int marginInPercent)
        {
            using (Devmasters.Imaging.InMemoryImage image = new InMemoryImage(imagedata))
            {
                if (marginInPercent > 99 || marginInPercent < 0)
                    throw new ArgumentOutOfRangeException("marginInPercent", "must be between 0 and 100");
                List<byte[]> facesImg = new List<byte[]>();
                CascadeClassifier _cascadeClassifier;
                _cascadeClassifier = new CascadeClassifier(modelFilename);
                //Image<Bgr, byte> img = Image<Bgr, byte>.FromIplImagePtr(image.Image.GetHbitmap);  

                //1. Convert ImageSharp.Image to byte array:
                byte[] imageBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    image.SaveAsPNG(ms);
                    imageBytes = ms.ToArray();
                }
                //dirty trick. init System.Drawing.Bitmp 
                Mat mat = new Mat();
                CvInvoke.Imdecode(imageBytes, Emgu.CV.CvEnum.ImreadModes.Color, mat);
                Image<Bgr, byte> img = mat.ToImage<Bgr, byte>();

                Image<Gray, byte> grayframe = img.Convert<Gray, byte>();
                var faces = _cascadeClassifier.DetectMultiScale(grayframe, 1.1, 10, Size.Empty); //the actual face detection happens here
                foreach (var face in faces)
                {
                    if (face.Width < minSize || face.Height < minSize)
                        continue;

                    int newX = face.X;
                    int newY = face.Y;
                    int changeX = (int)Math.Round(face.Width * ((double)marginInPercent / 100D));
                    int changeY = (int)Math.Round(face.Height * ((double)marginInPercent / 100D));
                    newX = newX - changeX / 2; newX = newX < 0 ? 0 : newX;
                    newY = newY - changeY / 2; newY = newY < 0 ? 0 : newY;
                    int newWidth = face.Width + changeX;
                    int newHeight = face.Height + changeY;

                    if (newX + newWidth > image.Image.Width)
                        newWidth = image.Image.Width - newX;
                    if (newY + newHeight > image.Image.Height)
                        newHeight = image.Image.Height - newY;

                    SixLabors.ImageSharp.Rectangle newFaceRect = new SixLabors.ImageSharp.Rectangle(newX, newY, newWidth, newHeight);

                    //img.Draw(newFaceRect, new Bgr(Color.BurlyWood), 3); //the detected face(s) is highlighted here using a box that is drawn around it/them
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        image.SaveAsJPEG(ms, 95);
                        using (InMemoryImage imi = new InMemoryImage(ms.ToArray()))
                        {
                            imi.Crop(newFaceRect);

                            using (System.IO.MemoryStream lms = new System.IO.MemoryStream())
                            {
                                imi.SaveAsJPEG(lms, 95);
                                facesImg.Add(lms.ToArray());
                            }
                        }
                    }
                }

                return facesImg;
            }
        }

    }
}
