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
        public float HeadingMinFontSize { get; set; } = 24f;
        public float FourLinerFontSize { get; set; } = 24f;
        public float YearFontSize { get; set; } = 28f;
        public string FontFamily { get; set; } = "Cabin";
        public string FontFamilySemiBold { get; set; } = "Cabin SemiBold";
        public string FontFamilyMedium { get; set; } = "Cabin Medium";

        public RectangleF HeadingPosition { get; set; } = new RectangleF(64, 29, 1470, 116);
        public RectangleF SSLText { get; set; } = new RectangleF(16, 300, 230, 39);
        public RectangleF SSLTextPosition { get; set; } = new RectangleF(405, 633, 1160, 114);

        public Byte[] GenerateImageByteArray(string headingText,
            string sslLabel, Color sslLabelColor, string sslText,
            decimal okPercentDay, string okStrDay, decimal okPercentWeek, string okStrWeek,
            decimal slowPercentDay, string slowStrDay, decimal slowPercentWeek, string slowStrWeek,
            decimal badPercentDay, string badStrDay, decimal badPercentWeek, string badStrWeek
        )
        {
            using (MemoryStream ms = GenerateImage(headingText,
                sslLabel, sslLabelColor, sslText,
                okPercentDay, okStrDay, okPercentWeek, okStrWeek,
                slowPercentDay, slowStrDay, slowPercentWeek, slowStrWeek,
                badPercentDay, badStrDay, badPercentWeek, badStrWeek
                ))
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
        public MemoryStream GenerateImage(string headingText,
            string sslLabel, Color sslLabelColor, string sslText,
            decimal okPercentDay, string okStrDay, decimal okPercentWeek, string okStrWeek,
            decimal slowPercentDay, string slowStrDay, decimal slowPercentWeek, string slowStrWeek,
            decimal badPercentDay, string badStrDay, decimal badPercentWeek, string badStrWeek
            )
        {
            using (Image image = Bitmap.FromFile(GetFileNameOnDisk()))
            {
                using (Graphics graphics = Graphics.FromImage(image))
                {
                    graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                    graphics.DrawImage(image, 0, 0);

                    FontFamily semiBold = MyFonts().Families
                        .Where(f => f.Name == this.FontFamilySemiBold)
                        .FirstOrDefault();

                    FontFamily regular = MyFonts().Families
                        .Where(f => f.Name == this.FontFamily)
                        .FirstOrDefault();

                    FontFamily medium = MyFonts().Families
                        .Where(f => f.Name == this.FontFamilyMedium)
                        .FirstOrDefault();


                    // nadpis
                    using (Font basicFont = new Font(semiBold, this.HeadingMinFontSize, FontStyle.Bold))
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
                                                               stringFormat, 0.8f))

                                graphics.DrawString(
                                    headingText,
                                    correctFont,
                                    Brushes.Black,
                                    this.HeadingPosition,
                                    stringFormat);
                        }
                    }

                    // ssltext
                    if (!string.IsNullOrEmpty(sslText))
                    {
                        using (Font font = new Font(medium, 16f, FontStyle.Regular))
                        {
                            using (StringFormat stringFormat = new StringFormat())
                            {
                                    stringFormat.LineAlignment = StringAlignment.Center;
                                    stringFormat.Alignment = StringAlignment.Near;
                                    stringFormat.Trimming = StringTrimming.EllipsisCharacter;

                                    // Vykreslení boxu
                                    graphics.DrawString(sslText,
                                        font,
                                        new SolidBrush(Color.FromArgb(80, 80, 80)),
                                        //Brushes.Black,
                                        this.SSLTextPosition,
                                        stringFormat);
                                
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(sslLabel))
                    {

                        // label
                        using (StringFormat stringFormat = new StringFormat())
                        {
                            stringFormat.LineAlignment = StringAlignment.Center;
                            stringFormat.Alignment = StringAlignment.Center;
                            using (Font basicFont = new Font(semiBold, 50f, FontStyle.Regular))
                            {
                                // Vykreslení SSLLabel
                                graphics.DrawString(sslLabel,
                                    basicFont,
                                    new SolidBrush(sslLabelColor),
                                    new RectangleF(263, 645, 111, 91),
                                    stringFormat);

                            }
                        }
                    } //label


                    //banners                   
                    using (StringFormat stringFormat = new StringFormat())
                    {
                        stringFormat.LineAlignment = StringAlignment.Center;
                        stringFormat.Alignment = StringAlignment.Center;
                        using (Font basicFont = new Font(semiBold, 20f, FontStyle.Regular))
                        {
                            using (Font smaller = new Font(regular, 14f, FontStyle.Regular))
                            {
                                //#497443
                                var color = Color.FromArgb(73, 116, 67);
                                var box = new Rectangle(186, 187, 1379, 114);
                                DrawRectangle(graphics, basicFont, smaller, stringFormat,
                                    color, box,
                                    okStrDay, okStrWeek, okPercentDay, okPercentWeek
                                    );

                                //slow #f19b38
                                color = Color.FromArgb(241, 155, 56);
                                box = new Rectangle(186, 334, 1379, 114);
                                DrawRectangle(graphics, basicFont, smaller, stringFormat,
                                    color, box,
                                    slowStrDay, slowStrWeek, slowPercentDay, slowPercentWeek
                                    );

                                //bad #CA4339
                                color = Color.FromArgb(202, 67, 57);
                                box = new Rectangle(186, 479, 1379, 114);
                                DrawRectangle(graphics, basicFont, smaller, stringFormat,
                                    color, box,
                                    badStrDay, badStrWeek, badPercentDay, badPercentWeek
                                    );
                            }
                        }
                    }



                    // datetime
                    using (StringFormat stringFormat = new StringFormat())
                    {
                        stringFormat.LineAlignment = StringAlignment.Center;
                        stringFormat.Alignment = StringAlignment.Far;
                        using (Font basicFont = new Font(semiBold, 15f, FontStyle.Regular))
                        {
                            // Vykreslení roku
                            graphics.DrawString("stav k " + DateTime.Now.ToString("d.M.yyyy HH:mm"),
                                basicFont,
                                new SolidBrush(Color.FromArgb(100, 100, 100)),
                                new RectangleF(1220, 736, 380, 30),
                                stringFormat);

                        }
                    }



                }

                MemoryStream ms = new MemoryStream();
                image.Save(ms, ImageFormat.Png);
                return ms;
            }
        }

        private void DrawRectangle(Graphics graphics, Font basicFont, Font smallerFont, StringFormat stringFormat,
            Color color, Rectangle box,
            string strDay, string strWeek, decimal percentDay, decimal percentWeek
            )
        {
            graphics.DrawRectangle(new Pen(Color.FromArgb(255, color)), box);
            // Vykreslení okDay
            graphics.FillRectangle(new SolidBrush(Color.FromArgb(125, color)), new Rectangle(box.X, box.Y, (int)(box.Width * percentDay), box.Height / 2));
            graphics.DrawString(strDay,
                basicFont,
                new SolidBrush(Color.Black),
                new RectangleF(box.X, box.Y, box.Width, box.Height / 2),
                stringFormat);
            graphics.DrawString("za 24 hodin",
                smallerFont,
                new SolidBrush(Color.FromArgb(80, 80, 80)),
                new RectangleF(box.X, box.Y, 180, box.Height / 2),
                stringFormat);

            // Vykreslení okWeek
            graphics.FillRectangle(new SolidBrush(Color.FromArgb(125, color)), new Rectangle(box.X, box.Y + (box.Height / 2), (int)(box.Width * percentWeek), box.Height / 2));
            graphics.DrawString(strWeek,
                basicFont,
                new SolidBrush(Color.Black),
                new RectangleF(box.X, box.Y + (box.Height / 2), box.Width, box.Height / 2),
                stringFormat);
            graphics.DrawString("za týden",
                smallerFont,
                new SolidBrush(Color.FromArgb(80, 80, 80)),
                new RectangleF(box.X, box.Y + (box.Height / 2), 150, box.Height / 2),
                stringFormat);

            graphics.DrawLine(new Pen(Color.FromArgb(255, color), 2), box.X, box.Y + (box.Height / 2), box.X + box.Width, box.Y + (box.Height / 2));

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
                                    StringFormat stringFormat,
                                    float sizeFactor = 1.0f)
        {
            SizeF extent = graphics.MeasureString(text, font, fontArea.Size, stringFormat);
            float hRatio = fontArea.Height / extent.Height;
            float wRatio = fontArea.Width / extent.Width;
            float ratio = (hRatio < wRatio) ? hRatio : wRatio;

            float fontSize = font.Size * ratio * sizeFactor;

            return new Font(font.FontFamily, fontSize, font.Style, font.Unit);
        }

        private readonly string _dataFolder;
        private string GetFileNameOnDisk()
        {

            return Path.Combine(_dataFolder, $"StatniWebyTemplate.png");
        }
    }

}
