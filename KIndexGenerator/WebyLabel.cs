using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;

namespace HlidacStatu.KIndexGenerator
{
    public class WebyLabel
    {
        public WebyLabel()
            : this(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location))
        { }

        public WebyLabel(string dataFolder)
        {
            _dataFolder = dataFolder;
        }
        // correct setting is kind of alchemy
        public float HeadingMinFontSize { get; set; } = 32f;
        public float FourLinerFontSize { get; set; } = 28f;
        public float YearFontSize { get; set; } = 28f;
        public string FontFamily { get; set; } = "Cabin SemiBold";
        public string FontFamilyFourLiner { get; set; } = "Cabin Medium";

        public RectangleF HeadingPosition { get; set; } = new RectangleF(20, 50, 960, 110);
        public RectangleF YearPosition { get; set; } = new RectangleF(793, 481, 147, 45);
        public RectangleF FourlinerPosition { get; set; } = new RectangleF(242, 625, 684, 192);

        public Byte[] GenerateImageByteArray(string headingText, string fourLinerText, string label, string year)
        {
            using (MemoryStream ms = GenerateImage(headingText, fourLinerText, label, year))
            {
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Creates png image and returns it as a memory stream. It needs to be disposed!
        /// </summary>
        /// <param name="headingText"></param>
        /// <param name="fourLinerText"></param>
        /// <returns></returns>
        public MemoryStream GenerateImage(string headingText, string fourLinerText, string label, string year)
        {
            using (Image image = Bitmap.FromFile(GetFileNameOnDisk(label)))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                    graphics.DrawImage(image, 0, 0);

                    FontFamily fontFamily = MyFonts().Families
                        .Where(f => f.Name == this.FontFamily)
                        .FirstOrDefault();

                    // nadpis
                    using (Font basicFont = new Font(fontFamily, this.HeadingMinFontSize, FontStyle.Bold))
                    {
                        using (StringFormat stringFormat = new StringFormat())
                        {
                            stringFormat.LineAlignment = StringAlignment.Near;
                            stringFormat.Alignment = StringAlignment.Center;
                            stringFormat.Trimming = StringTrimming.EllipsisCharacter;
                            //stringFormat.FormatFlags = StringFormatFlags.NoWrap;

                            using (Font correctFont = SizeFont(headingText,
                                                               graphics,
                                                               basicFont,
                                                               this.HeadingPosition,
                                                               stringFormat))
                                graphics.DrawString(
                                    headingText,
                                    correctFont,
                                    Brushes.Black,
                                    this.HeadingPosition,
                                    stringFormat);
                        }
                    }

                    FontFamily fontFamilyFourLiner = MyFonts().Families
                        .Where(f => f.Name == this.FontFamilyFourLiner)
                        .FirstOrDefault();
                    // Box dole - ten bych nechal vždy velikostí 30, tady neresizuju text
                    using (Font fourLinerFont = new Font(fontFamilyFourLiner, this.FourLinerFontSize, FontStyle.Regular))
                    {
                        using (StringFormat stringFormat = new StringFormat())
                        {
                            stringFormat.LineAlignment = StringAlignment.Center;
                            stringFormat.Alignment = StringAlignment.Near;
                            stringFormat.Trimming = StringTrimming.EllipsisCharacter;

                            // Vykreslení boxu
                            graphics.DrawString(fourLinerText,
                                fourLinerFont,
                                new SolidBrush(Color.FromArgb(60, 60, 60)),
                                //Brushes.Black,
                                this.FourlinerPosition,
                                stringFormat);
                        }
                    }

                    // Rok - ten bych nechal vždy velikostí 36, tady neresizuju text
                    using (Font yearFont = new Font(fontFamily, this.YearFontSize, FontStyle.Regular))
                    {
                        using (StringFormat stringFormat = new StringFormat())
                        {
                            stringFormat.LineAlignment = StringAlignment.Center;
                            stringFormat.Alignment = StringAlignment.Far;

                            // Vykreslení roku
                            graphics.DrawString(year.ToString(),
                                yearFont,
                                Brushes.Black,
                                this.YearPosition,
                                stringFormat);
                        }
                    }
                }

                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                return ms;
            }
        }

        private PrivateFontCollection MyFonts()
        {
            PrivateFontCollection pfc = new PrivateFontCollection();
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-Bold.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-BoldItalic.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-Italic.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-Medium.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-MediumItalic.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-Regular.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-SemiBold.ttf"));
            pfc.AddFontFile(Path.Combine(_dataFolder, "Cabin-SemiBoldItalic.ttf"));
            return pfc;
        }

        private static Font SizeFont(string text,
                                    Graphics graphics,
                                    Font font,
                                    RectangleF fontArea,
                                    StringFormat stringFormat)
        {
            SizeF extent = graphics.MeasureString(text, font, fontArea.Size, stringFormat);
            float hRatio = fontArea.Height / extent.Height;
            float wRatio = fontArea.Width / extent.Width;
            float ratio = (hRatio < wRatio) ? hRatio : wRatio;

            float fontSize = font.Size * ratio;

            return new Font(font.FontFamily, fontSize, font.Style, font.Unit);
        }

        private readonly string _dataFolder;
        private string GetFileNameOnDisk(string label)
        {

            return Path.Combine(_dataFolder, $"Label{label}.png");
        }
    }

}
