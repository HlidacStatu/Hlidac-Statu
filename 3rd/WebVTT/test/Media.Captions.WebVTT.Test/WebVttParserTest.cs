// ---------------------------------------------------------------------------
// <copyright file="WebVttParserTest.cs" owner="svm-git">
//
//  Copyright (c) 2018 svm-git
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.
//
// </copyright>
// ---------------------------------------------------------------------------

namespace Media.Captions.WebVTT.Test
{
    using System;
    using System.IO;

    using Media.Captions.WebVTT;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebVttParserTest
    {
        [TestMethod]
        public void ParseCaptions()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleCaption, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseRegions()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleRegions, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseMetadata()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleMetadata, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseChapter()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleChapters, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfShortSignature_Throws()
        {
            string vtt = "WEB";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfIncorrectSignature_Throws()
        {
            string vtt = "WEBvtt";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfIncorrectSuffixSignature_Throws()
        {
            string vtt = "WEBVTT_";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseEmptyFile()
        {
            string vtt = @"WEBVTT  
    
   
   " + "\t\t\t\r\n";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.IsNotNull(captions);
                Assert.IsNull(captions.Regions);
                Assert.IsNull(captions.Cues);
                Assert.IsNull(captions.Styles);
            }
        }

        [TestMethod]
        public void ParseHoursInCueTime()
        {
            string vtt = 
@"WEBVTT

00:35.123-->12345:59:59.999
Caption with hourly duration";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                Assert.IsNotNull(captions);
                Assert.IsNull(captions.Regions);
                Assert.IsNotNull(captions.Cues);
                Assert.IsNull(captions.Styles);

                Assert.AreEqual(1, captions.Cues.Length);
                Assert.AreEqual(TimeSpan.Parse("00:00:35.123"), captions.Cues[0].Start);
                Assert.AreEqual(TimeSpan.Parse("514.09:59:59.999"), captions.Cues[0].End);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfNoMinutesInTiming_Throw()
        {
            string vtt =
@"WEBVTT

35.123 --> 59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfNoFractionsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

00:35 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooManyDigitsInMinutesInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:000:35.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooManyDigitsInSecondsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:00:000.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfOnlyFractionsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfHoursTooLargeInTiming_Throws()
        {
            string vtt =
@"WEBVTT

12345678901234567890:00:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooManyFractionDigitsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

00:00.0000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooManyMinutesInTiming_Throws()
        {
            string vtt =
@"WEBVTT

60:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooManySecondsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

00:70.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfHasHours_And_TooManyMinutesInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:60:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfHasHours_And_TooManySecondsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:00:70.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooFewSecondsDigitsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

00:0.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfHasHours_And_TooFewSecondsDigitsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:00:0.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfTooFewMinutessDigitsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfHasHours_And_TooFewMinutessDigitsInTiming_Throws()
        {
            string vtt =
@"WEBVTT

0:0:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfNonDigitsInTiming_Throw()
        {
            string vtt =
@"WEBVTT

0:__:00.000 --> 12345:59:59.999
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfEndLessThanStart_Throws()
        {
            string vtt =
@"WEBVTT

0:10:00.000 --> 0:00:00.000
Bad caption";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfRegionAfterCue_Throws()
        {
            string vtt =
@"WEBVTT

0:00:00.000 --> 0:10:00.000
Some caption

REGION
Some:data";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfStyleAfterCue_Throws()
        {
            string vtt =
@"WEBVTT

0:00:00.000 --> 0:10:00.000
Some caption

STYLE
Some data";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseSingleLineCue()
        {
            string testSettings = "start:10%";
            string vtt =
@"WEBVTT

0:00:00.000 --> 0:10:00.000 " + testSettings;

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Cues);
                Assert.IsTrue(captions.Cues.Length == 1);
                Assert.IsNotNull(captions.Cues[0].RawSettings);
                Assert.AreEqual(testSettings, captions.Cues[0].RawSettings);
                Assert.IsNull(captions.Cues[0].RawContent);
            }
        }

        [TestMethod]
        public void ParseRegion()
        {
            string region = @"id:fred
width:40%
lines:3
regionanchor:0%,100%
viewportanchor:10%,90%
scroll:up";
            string vtt =
@"WEBVTT

REGION
" + region +
@"
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Regions);
                Assert.IsTrue(captions.Regions.Length == 1);
                Assert.IsNotNull(captions.Regions[0].RawContent);
                Assert.AreEqual(region, captions.Regions[0].RawContent);

                Assert.AreEqual("fred", captions.Regions[0].Id, "IDs are different.");
                Assert.AreEqual(3, captions.Regions[0].Lines.Value, "Lines are different.");
                Assert.AreEqual(true, captions.Regions[0].Scroll.Value, "Scrolls are different.");
                Assert.AreEqual(40.0, captions.Regions[0].WidthPercent.Value, "Widths are different.");
                Assert.AreEqual(0.0, captions.Regions[0].RegionAnchor.Value.XPercent, "Region anchor Xs are different.");
                Assert.AreEqual(100.0, captions.Regions[0].RegionAnchor.Value.YPercent, "Region anchor Ys are different.");
                Assert.AreEqual(10.0, captions.Regions[0].ViewPortAnchor.Value.XPercent, "Viewport anchor Xs are different.");
                Assert.AreEqual(90.0, captions.Regions[0].ViewPortAnchor.Value.YPercent, "Viewport anchor Ys are different.");
            }
        }

        [TestMethod]
        public void ParseRegionWithInvalidSettings()
        {
            string region = @"id:fred
width:40
lines:_
regionanchor:0,100%
viewportanchor:10,90%
scroll:down";
            string vtt =
@"WEBVTT

REGION
" + region + @"
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Regions);
                Assert.IsTrue(captions.Regions.Length == 1);
                Assert.IsNotNull(captions.Regions[0].RawContent);
                Assert.AreEqual(region, captions.Regions[0].RawContent);

                Assert.AreEqual("fred", captions.Regions[0].Id, "IDs are different.");
                Assert.IsNull(captions.Regions[0].Lines);
                Assert.AreEqual(false, captions.Regions[0].Scroll.Value, "Scrolls are different.");
                Assert.IsNull(captions.Regions[0].WidthPercent);
                Assert.IsNull(captions.Regions[0].RegionAnchor);
                Assert.IsNull(captions.Regions[0].ViewPortAnchor);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfNoColonInSettings_Throws()
        {
            string region = @"id-fred";
            string vtt =
@"WEBVTT

REGION
" + region + @"
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidDataException))]
        public void IfNoValueAfterColonInSettings_Throws()
        {
            string region = @"width:";
            string vtt =
@"WEBVTT

REGION
" + region + @"
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        public void ParseStyle()
        {
            string style = @"::cue {
  background-image: linear-gradient(to bottom, dimgray, lightgray);
  color: papayawhip;
}";
            string vtt =
@"WEBVTT

STYLE
" + style +
@"
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Styles);
                Assert.IsTrue(captions.Styles.Length == 1);
                Assert.IsNotNull(captions.Styles[0].RawContent);
                Assert.AreEqual(style, captions.Styles[0].RawContent);
            }
        }

        [TestMethod]
        public void ParseCaptionWithSettings()
        {
            string caption = @"align:right size:50% vertical:lr line:3%,center position:15%,line-right region:r";
            string vtt =
@"WEBVTT

00:30.000 --> 00:31.500 " + caption + @"
<v Roger Bingham>We are in New York City";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Cues);
                Assert.IsTrue(captions.Cues.Length == 1);
                Assert.IsNotNull(captions.Cues[0].RawSettings);
                Assert.AreEqual(caption, captions.Cues[0].RawSettings);

                Assert.AreEqual("r", captions.Cues[0].Region, "Regions are different.");
                Assert.AreEqual(TextAlignment.Right, captions.Cues[0].Alignment.Value, "Alignments are different.");
                Assert.AreEqual(50, captions.Cues[0].SizePercent.Value, "Sizes are different.");
                Assert.AreEqual(VerticalTextLayout.LeftToRight, captions.Cues[0].Vertical.Value, "Verticals are different.");
                Assert.AreEqual(LineAlignment.Center, captions.Cues[0].Line.Value.Alignment.Value, "Line.Alignments are different.");
                Assert.AreEqual(3.0, captions.Cues[0].Line.Value.Percent.Value, "Line.Percents are different.");
                Assert.AreEqual(PositionAlignment.LineRight, captions.Cues[0].Position.Value.Alignment.Value, "Position.Alignments are different.");
                Assert.AreEqual(15.0, captions.Cues[0].Position.Value.PositionPercent.Value, "Position.Percents are different.");
            }
        }

        [TestMethod]
        public void ParseCaptionIgnoreBadSettings()
        {
            string caption = @"align:bad size:50 vertical:no line:%,center position:15%_line-right random:r";
            string vtt =
@"WEBVTT

00:30.000 --> 00:31.500 " + caption + @"
<v Roger Bingham>We are in New York City";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions);
                Assert.IsNotNull(captions.Cues);
                Assert.IsTrue(captions.Cues.Length == 1);
                Assert.IsNotNull(captions.Cues[0].RawSettings);
                Assert.AreEqual(caption, captions.Cues[0].RawSettings);

                Assert.IsNull(captions.Cues[0].Alignment);
                Assert.IsNull(captions.Cues[0].SizePercent);
                Assert.IsNull(captions.Cues[0].Vertical);
                Assert.IsNull(captions.Cues[0].Line);
                Assert.IsNull(captions.Cues[0].Position);
            }
        }

        [TestMethod]
        public void ParseItalicsSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<i> italics </i>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Italics, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be italics.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual(" italics ", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseBoldSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<b> bold</b>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Bold, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be bold.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual(" bold", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseUnderlineSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<u>underline </u>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Underline, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be underline.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual("underline ", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseClassSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<c>class</c>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Class, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be class.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual("class", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseLanguageSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<lang en-us>other language</lang>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Language, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be language.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.AreEqual("en-us", captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation is invalid.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual("other language", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseVoiceSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<v Speaker in Space > Something interesting";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Voice, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be voice.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.AreEqual("Speaker in Space ", captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation is invalid.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual(" Something interesting", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseRubySpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<ruby>ruby</ruby>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Ruby, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be ruby.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual("ruby", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseRubyTextSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<rt>ruby_text</rt>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.RubyText, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be ruby text.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[0].Children.Length, "Cues.Content[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[0].Children[0], "Cues.Content[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Children[0].Type, "Cues.Content[0].Children[0].Type is invalid.");
                Assert.AreEqual("ruby_text", captions.Cues[0].Content[0].Children[0].Text, "Cues.Content[0].Children[0].Text is invalid.");
            }
        }

        [TestMethod]
        public void ParseSpanWithManyClasses()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<v.very.loud.many.classes Voice Name >";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Voice, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be voice.");
                Assert.IsNull(captions.Cues[0].Content[0].Text, "Cues.Content[0].Text must be null.");
                Assert.AreEqual("Voice Name ", captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation is invalid.");
                Assert.IsNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must be null.");
                
                Assert.IsNotNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must not be null.");
                CollectionAssert.AreEquivalent(
                    new string[] { "very", "loud", "many", "classes" },
                    captions.Cues[0].Content[0].Classes,
                    "Cues.Content[0].Classes are invalid.");
            }
        }

        [TestMethod]
        public void ParseNestedSpans()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
Some city with <i.foreignphrase><lang en>playground</lang></i> in it";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNotNull(captions.Cues[0].Content, "Cues.Content must not be null.");
                Assert.AreEqual(3, captions.Cues[0].Content.Length, "Cues.Content.Length is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[0], "Cues.Content[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[0].Type, "Cues.Content[0] must be text.");
                Assert.AreEqual("Some city with ", captions.Cues[0].Content[0].Text, "Cues.Content[0].Text is invalid.");
                Assert.IsNull(captions.Cues[0].Content[0].Annotation, "Cues.Content[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Children, "Cues.Content[0].Children must be null.");
                Assert.IsNull(captions.Cues[0].Content[0].Classes, "Cues.Content[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[1], "Cues.Content[1] must not be null.");
                Assert.AreEqual(SpanType.Italics, captions.Cues[0].Content[1].Type, "Cues.Content[1] must be italics.");
                Assert.IsNull(captions.Cues[0].Content[1].Text, "Cues.Content[1].Text must be null.");
                Assert.IsNull(captions.Cues[0].Content[1].Annotation, "Cues.Content[1].Annotation must be null.");
                Assert.IsNotNull(captions.Cues[0].Content[1].Classes, "Cues.Content[1].Classes must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[1].Classes.Length, "Cues.Content[1].Classes.Length is invalid.");
                Assert.AreEqual("foreignphrase", captions.Cues[0].Content[1].Classes[0], "Cues.Content[1].Classes[0] is invalid.");

                Assert.IsNotNull(captions.Cues[0].Content[1].Children, "Cues.Content[1].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[1].Children.Length, "Cues.Content[1].Children.Length is invalid.");
                Assert.AreEqual(SpanType.Language, captions.Cues[0].Content[1].Children[0].Type, "Cues.Content[1].Children[0] must be Language.");
                Assert.IsNull(captions.Cues[0].Content[1].Children[0].Text, "Cues.Content[1].Children[0].Text must be null.");
                Assert.AreEqual("en", captions.Cues[0].Content[1].Children[0].Annotation, "Cues.Content[1].Children[0].Annotation is invalid.");
                Assert.IsNull(captions.Cues[0].Content[1].Children[0].Classes, "Cues.Content[1].Children[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[1].Children[0].Children, "Cues.Content[1].Children[0].Children must not be null.");
                Assert.AreEqual(1, captions.Cues[0].Content[1].Children[0].Children.Length, "Cues.Content[1].Children[0].Children.Length is invalid.");
                Assert.IsNotNull(captions.Cues[0].Content[1].Children[0].Children[0], "Cues.Content[1].Children[0].Children[0] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[1].Children[0].Children[0].Type, "Cues.Content[1].Children[0].Children[0] must be text.");
                Assert.AreEqual("playground", captions.Cues[0].Content[1].Children[0].Children[0].Text, "Cues.Content[1].Children[0].Children[0].Text is invalid.");
                Assert.IsNull(captions.Cues[0].Content[1].Children[0].Children[0].Annotation, "Cues.Content[1].Children[0].Children[0].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[1].Children[0].Children[0].Children, "Cues.Content[1].Children[0].Children[0].Children must be null.");
                Assert.IsNull(captions.Cues[0].Content[1].Children[0].Children[0].Classes, "Cues.Content[1].Children[0].Children[0].Classes must be null.");

                Assert.IsNotNull(captions.Cues[0].Content[2], "Cues.Content[2] must not be null.");
                Assert.AreEqual(SpanType.Text, captions.Cues[0].Content[2].Type, "Cues.Content[2] must be text.");
                Assert.AreEqual(" in it", captions.Cues[0].Content[2].Text, "Cues.Content[2].Text is invalid.");
                Assert.IsNull(captions.Cues[0].Content[2].Annotation, "Cues.Content[2].Annotation must be null.");
                Assert.IsNull(captions.Cues[0].Content[2].Children, "Cues.Content[2].Children must be null.");
                Assert.IsNull(captions.Cues[0].Content[2].Classes, "Cues.Content[2].Classes must be null.");
            }
        }

        [TestMethod]
        public void IfClassEmpty_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<v.. Voice Name >";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNewLineInStartTag_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<i
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNewLineInTagClass_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<i.class
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNewLineAfterLessThen_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNewLineAfterLessThenInTagEnd_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<i>italics<
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfWrongTagEnd_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<i>italics</b>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNewLineAfterAnnotation_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<v Speaker ";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfNoGreaterThenInTagEnd_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<ruby> ruby </ruby
";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }

        [TestMethod]
        public void IfInvalidSpan_IgnoresSpan()
        {
            string vtt =
@"WEBVTT

04:05.001 --> 04:07.800
<random> span </random>";

            using (var reader = new StringReader(vtt))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();
                Assert.IsNotNull(captions, "Captions must not be null.");
                Assert.IsNotNull(captions.Cues, "Captions.Cues must not be null.");
                Assert.IsTrue(captions.Cues.Length == 1, "Length must be 1.");

                Assert.IsNull(captions.Cues[0].Content, "Cues.Content must not be null.");
            }
        }
    }
}
