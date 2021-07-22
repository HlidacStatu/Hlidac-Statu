using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

using Svg;


namespace HlidacStatu.KIndexGenerator
{
    public class PercentileBanner
    {
        Dictionary<int, decimal> vals = null;
        SvgDocument sdoc = null;

        const int defaultImageWidth = 1439;

        private int width = 1050;
        private int widthText = 530;
        private int widthRect = 230;
        private int widthLines = 1610;

        private decimal range = 0;
        private decimal nearMin = 0;
        private decimal nearMax = 0;

        public decimal CurrValue { get; private set; }

        public PercentileBanner(
            decimal currValue,
            decimal min, decimal percentile10, decimal percentile25,
            decimal percentile50, decimal percentile75,
            decimal percentile90, decimal max
            )
: this(currValue,
      new Dictionary<int, decimal>() { { 0, min }, { 10, percentile10 }, { 25, percentile25 }, { 50, percentile50 }, { 75, percentile75 }, { 90, percentile90 }, { 100, max } }
      , System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
        { }

        public PercentileBanner(
            decimal currValue,
    decimal min, decimal percentile10, decimal percentile25,
    decimal percentile50, decimal percentile75,
    decimal percentile90, decimal max,
    string dataFolder
    )
        : this(currValue, new Dictionary<int, decimal>() { { 0, min }, { 10, percentile10 }, { 25, percentile25 }, { 50, percentile50 }, { 75, percentile75 }, { 90, percentile90 }, { 100, max } }
            , dataFolder)
        { }

        public PercentileBanner(decimal currValue, Dictionary<int, decimal> percentilesValues)
        : this(currValue, percentilesValues, System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
        { }


        public PercentileBanner(decimal currValue,Dictionary<int, decimal> percentilesValues, string dataFolder)
        {
            this.CurrValue = currValue;

            this.vals = percentilesValues;
            sdoc = SvgDocument.Open<SvgDocument>(System.IO.Path.Combine(dataFolder, "PercentileBanner.svg"));

            decimal perc80range = vals[90] - vals[10];
            decimal near = 0.125m * perc80range;

            nearMin = Math.Max(vals[0], vals[10] - near);
            nearMax = Math.Min(vals[100], vals[90] + near);

            range = nearMax - nearMin;

            CreateSVG(this.CurrValue);

        }

        private int CalcTransformX(decimal value, bool text = false)
        {
            return CalcTransformX(value, text ? widthText : width);
        }

        private int CalcTransformX(decimal value, int width)
        {
            if (value > nearMax)
                value = nearMax;
            if (value < nearMin)
                value = nearMin;

            decimal perc = (value - nearMin) / range;
            if (perc < 0m)
                perc = 0m;

            decimal transfX = (width) * perc;

            return (int)transfX;
        }

        private void MoveElement(string id, decimal valueForElement)
        {
            int xMove = CalcTransformX(valueForElement);
            var el = sdoc.GetElementById(id);
            if (el.GetType() == typeof(SvgText))
            {
                el = el.Parent;
                xMove = CalcTransformX(valueForElement, true);
            }
            if (el.Transforms == null)
                el = el.Parent;
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(xMove));
        }


        private void ChangeText(string id, string text)
        {
            var txt = sdoc.GetElementById<SvgText>(id);
            //.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(valueForElement)));
            txt.Text = text;

        }

        private void CreateSVG(decimal vysledek)
        {
            MoveElement("10perc", vals[10]);
            ChangeText("t10", vals[10].ToString("N2"));
            MoveElement("50perc", vals[50]);
            ChangeText("t50", vals[50].ToString("N2"));
            MoveElement("90perc", vals[90]);
            ChangeText("t90", vals[90].ToString("N2"));

            MoveElement("t25", vals[25]);
            ChangeText("t25", vals[25].ToString("N2"));
            MoveElement("t75", vals[75]);
            ChangeText("t75", vals[75].ToString("N2"));

            MoveElement("vysledek", vysledek);
            ChangeText("vysledekText", vysledek.ToString("N2"));

            var el = sdoc.GetElementById("rozsah_group").Parent;
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(vals[25], widthRect)));

            SvgRectangle rect = sdoc.GetElementById<SvgRectangle>("seda");
            rect.Width = CalcTransformX(vals[75], widthRect) - CalcTransformX(vals[25], widthRect);

            el = sdoc.GetElementById("rozsah").Parent;
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(vals[25], widthText)));

            el = sdoc.GetElementById("rozsahLeft");
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(vals[25], widthLines)));
            el = sdoc.GetElementById("rozsahRight");
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(vals[75], widthLines)));
            el = sdoc.GetElementById("rozsahLine");
            el.Transforms.Add(new Svg.Transforms.SvgTranslate(CalcTransformX(vals[25])));
            SvgRectangle elp = (SvgRectangle)el.Children.First();
            elp.Width = CalcTransformX(vals[75]) - CalcTransformX(vals[25]);



            
            //this.BitMap().Save(@"\!!\!sample1.png");
            //System.IO.File.WriteAllText(@"\!!\!sample1.svg", Svg());
            //sdoc.Draw(Svg.SvgRenderer)
        }

        public string Svg()
        {
            string ret = "";
            using (System.IO.MemoryStream mem = new MemoryStream())
            {
                sdoc.Write(mem, true);
                ret= Encoding.UTF8.GetString(mem.ToArray());
            }
            return ret
                .Replace("xmlns:xml=\"http://www.w3.org/XML/1998/namespace\"", "")
                .Replace(" d1p1:space=\"preserve\"", "")
                .Replace("xmlns:d1p1=\"http://www.w3.org/XML/1998/namespace\"","")
                ; // error on line 3 at column 352: xml namespace URI mapped to wrong prefix fix

        }

        public System.Drawing.Bitmap BitMap(int width = defaultImageWidth)
        {
            decimal perc = width / defaultImageWidth;
            int crop = (int)(350m * perc);

            var bmp = sdoc.Draw(width, 0);
            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(crop, 0, bmp.Width - crop, bmp.Height);
            return bmp.Clone(rect, bmp.PixelFormat);

            //var el = sdoc.GetElementById("crop");

            //SizeF dimensions = new SizeF(1157, 889);
            //var bitmap = new System.Drawing.Bitmap((int)Math.Round(dimensions.Width), (int)Math.Round(dimensions.Height));
            //el.RenderElement(SvgRenderer.FromImage(bitmap));

            //return bitmap;
            
        }

        public MemoryStream PngStream()
        {
            MemoryStream ms = new MemoryStream();
            this.BitMap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms;

        }
        public byte[] PngArray()
        {
            using (MemoryStream ms = this.PngStream())
            {
                return ms.ToArray();
            }
        }
    }
}
