// ---------------------------------------------------------------------------
// <copyright file="WebVttSerializerTest.cs" owner="svm-git">
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
    using System.Text;

    using Media.Captions.WebVTT;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class WebVttSerializerTest
    {
        [TestMethod]
        public void WriteRegions()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleRegions, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                StringBuilder sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    WebVttSerializer.SerializeAsync(captions, writer).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                using (var reader2 = new StringReader(sb.ToString()))
                {
                    var captions2 = WebVttParser.ReadMediaCaptionsAsync(reader2).ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.IsNotNull(captions2.Cues, "Cues are null after serialization.");
                    Assert.AreEqual(captions.Cues.Length, captions2.Cues.Length, "Cue counts do not match.");

                    Assert.IsNotNull(captions2.Regions, "Regions are null after serialization.");
                    Assert.AreEqual(captions.Regions.Length, captions2.Regions.Length, "Region counts do not match.");
                }
            }
        }

        [TestMethod]
        public void WriteCaptions()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleCaption, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                StringBuilder sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    WebVttSerializer.SerializeAsync(captions, writer).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                using (var reader2 = new StringReader(sb.ToString()))
                {
                    var captions2 = WebVttParser.ReadMediaCaptionsAsync(reader2).ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.IsNotNull(captions2.Cues, "Cues are null after serialization.");
                    Assert.AreEqual(captions.Cues.Length, captions2.Cues.Length, "Cue counts do not match.");

                    Assert.IsNull(captions2.Regions, "Regions are not null after serialization.");
                }
            }
        }

        [TestMethod]
        public void WriteChapters()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleCaption, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                StringBuilder sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    WebVttSerializer.SerializeAsync(captions, writer).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                using (var reader2 = new StringReader(sb.ToString()))
                {
                    var captions2 = WebVttParser.ReadMediaCaptionsAsync(reader2).ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.IsNotNull(captions2.Cues, "Cues are null after serialization.");
                    Assert.AreEqual(captions.Cues.Length, captions2.Cues.Length, "Cue counts do not match.");

                    Assert.IsNull(captions2.Regions, "Regions are not null after serialization.");
                }
            }
        }

        [TestMethod]
        public void WriteMetadata()
        {
            using (var reader = new StreamReader(new MemoryStream(Properties.Resources.SampleCaption, false)))
            {
                var captions = WebVttParser.ReadMediaCaptionsAsync(reader).ConfigureAwait(false).GetAwaiter().GetResult();

                StringBuilder sb = new StringBuilder();
                using (var writer = new StringWriter(sb))
                {
                    WebVttSerializer.SerializeAsync(captions, writer).ConfigureAwait(false).GetAwaiter().GetResult();
                }

                using (var reader2 = new StringReader(sb.ToString()))
                {
                    var captions2 = WebVttParser.ReadMediaCaptionsAsync(reader2).ConfigureAwait(false).GetAwaiter().GetResult();

                    Assert.IsNotNull(captions2.Cues, "Cues are null after serialization.");
                    Assert.AreEqual(captions.Cues.Length, captions2.Cues.Length, "Cue counts do not match.");

                    Assert.IsNull(captions2.Regions, "Regions are not null after serialization.");
                }
            }
        }

        [TestMethod]
        public void WriteEmptyContent()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(new BaseBlock[0], writer).ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteEmptyRegion()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] { new RegionDefinition() },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

REGION
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteRegion()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] 
                    { 
                        new RegionDefinition()
                        {
                            Id = "Id",
                            Lines = 5,
                            RegionAnchor = new Anchor() { XPercent = 1.2, YPercent = 15.375 },
                            ViewPortAnchor = new Anchor() { XPercent = 99.99, YPercent = 0.01 },
                            WidthPercent = 98.76,
                            Scroll = true
                        }
                    },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

REGION
id:Id
lines:5
width:98.76%
regionanchor:1.2%,15.375%
viewportanchor:99.99%,0.01%
scroll:up
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteEmptyComment()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] { new Comment() },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteInlineComment()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] { Comment.Create("Test inline comment.") },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

NOTE Test inline comment.
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteMultiLineComment()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] { Comment.Create("Test comment.\r\nWith new line.\r\n\r\n") },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

NOTE
Test comment.
With new line.
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteStyle()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        Style.Create(@"::cue {
  background-image: linear-gradient(to bottom, dimgray, lightgray);
  color: papayawhip;
}")
                    },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

STYLE
::cue {
  background-image: linear-gradient(to bottom, dimgray, lightgray);
  color: papayawhip;
}
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteSingleLineCue()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[] 
                    {
                        new Cue()
                        {
                            Alignment = TextAlignment.Start,
                            Start = TimeSpan.FromSeconds(9.25),
                            End = new TimeSpan(1, 2, 3, 4, 56),
                            Id = "Cue Id",
                            Line = new LineSettings() { Alignment = LineAlignment.Center, Percent = 12.34 },
                            Position = new PositionSettings() { Alignment = PositionAlignment.LineRight, PositionPercent = 43.21 },
                            Region = "Region",
                            SizePercent = 100.0,
                            Vertical = VerticalTextLayout.LeftToRight,
                            Content = new Span[] 
                            {
                                new Span() 
                                {
                                    Type = SpanType.Voice,
                                    Classes = new string[] { "one", "two", "three" },
                                    Annotation = "Cue voice",
                                    Children = new Span[]
                                    {
                                        new Span() { Type = SpanType.Text, Text = "Prefix" },
                                        new Span() { Type = SpanType.Italics, Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Italic" } } },
                                        new Span() { Type = SpanType.Underline, Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Underline" } } },
                                        new Span() { Type = SpanType.Bold, Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Bold" } } },
                                        new Span() { Type = SpanType.Class, Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Class" } } },
                                        new Span() { Type = SpanType.Language, Annotation = "en-us", Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Language" } } },
                                    }
                                }
                            }
                        }
                    },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

Cue Id
00:00:09.250 --> 1:02:03:04.056 region:Region align:start line:12.34%,center position:43.21%,line-right size:100% vertical:lr
<v.one.two.three Cue voice>Prefix<i>Italic</i><u>Underline</u><b>Bold</b><c>Class</c><lang en-us>Language</lang></v>
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteMultiLineCue()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue()
                        {
                            Start = TimeSpan.FromSeconds(10),
                            End = TimeSpan.FromSeconds(20),
                            Content = new Span[]
                            {
                                new Span() { Type = SpanType.Text, Text = "First line." },
                                new Span() { Type = SpanType.Text, Text = "Second line." },
                                new Span() { Type = SpanType.Voice, Annotation = "Voice", Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Voice text." } } },
                                new Span() { Type = SpanType.Ruby, Children = new Span[] { new Span() { Type = SpanType.RubyText, Children = new Span[] { new Span() { Type = SpanType.Text, Text = "Ruby text" } }  } } },
                            }
                        }
                    },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

00:00:10.000 --> 00:00:20.000
First line.
Second line.
<v Voice>Voice text.</v>
<ruby><rt>Ruby text</rt></ruby>
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        public void WriteMediaCaptionsWithOutOfOrderCues()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new MediaCaptions
                    {
                        Cues = new Cue[] {
                            new Cue()
                            {
                                Start = TimeSpan.FromSeconds(30),
                                End = TimeSpan.FromSeconds(40),
                                Content = new Span[]
                                {
                                    new Span() { Type = SpanType.Text, Text = "Second cue." },
                                }
                            },
                            new Cue()
                            {
                                Start = TimeSpan.FromSeconds(10),
                                End = TimeSpan.FromSeconds(20),
                                Content = new Span[]
                                {
                                    new Span() { Type = SpanType.Text, Text = "First cue." },
                                }
                            },
                            new Cue()
                            {
                                Start = TimeSpan.FromSeconds(30),
                                End = TimeSpan.FromSeconds(50),
                                Content = new Span[]
                                {
                                    new Span() { Type = SpanType.Text, Text = "Third cue." },
                                }
                            }
                        }
                    },
                    writer)
                        .ConfigureAwait(false).GetAwaiter().GetResult();

                string expected = @"WEBVTT

00:00:10.000 --> 00:00:20.000
First cue.

00:00:30.000 --> 00:00:40.000
Second cue.

00:00:30.000 --> 00:00:50.000
Third cue.
";

                Assert.AreEqual(expected, sb.ToString());
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IfNullWriter_Throws()
        {
            WebVttSerializer.SerializeAsync((MediaCaptions)null, null)
                .ConfigureAwait(false).GetAwaiter().GetResult();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfNullBlock_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(new BaseBlock[] { null }, writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfRegionAfterCue_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() { Start = TimeSpan.FromSeconds(0), End = TimeSpan.FromSeconds(1) },
                        new RegionDefinition()
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfStyleAfterCue_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() { Start = TimeSpan.FromSeconds(0), End = TimeSpan.FromSeconds(1) },
                        new Style()
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueTimesOutOfOrder_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() { Start = TimeSpan.FromSeconds(10), End = TimeSpan.FromSeconds(20) },
                        new Cue() { Start = TimeSpan.FromSeconds(0), End = TimeSpan.FromSeconds(1) },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfUnknownBlock_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new DerivedBlock()
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCommentWithArrow_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        Comment.Create("Comments with --> are not allowed.")
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfStyleWithArrow_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        Style.Create("Styles with --> are not allowed.")
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueEndTimeLessThanStartTime_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() { Start = TimeSpan.FromSeconds(10), End = TimeSpan.FromSeconds(2) },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueRegionNameHasWhiteSpace_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() { Start = TimeSpan.FromSeconds(1), End = TimeSpan.FromSeconds(2), Region = "Region with space" },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueWithInvalidLine_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Line = new LineSettings()
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueWithNullSpan_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Content = new Span[] { null }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueTextHasArrow_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Content = new Span[] { new Span() { Type = SpanType.Text, Text = "Text with --> is not allowed."} }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void IfCueSpanClassHasWhiteSpace_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Content = new Span[] { new Span() { Type = SpanType.Italics, Classes = new string[] { "White\tspace not\rallowed\n" }} }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfCueAlignmentInvalid_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Alignment = (TextAlignment)1234
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfCueLineAlignmentInvalid_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Line = new LineSettings() { Percent = 0, Alignment = (LineAlignment)4321 }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfCuePositionAlignmentInvalid_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Position = new PositionSettings() { Alignment = (PositionAlignment)4321 }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfCueVerticalAlignmentInvalid_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Vertical = (VerticalTextLayout)5678
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void IfCueSpanTypeInvalid_Throws()
        {
            StringBuilder sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WebVttSerializer.SerializeAsync(
                    new BaseBlock[]
                    {
                        new Cue() 
                        {
                            Start = TimeSpan.FromSeconds(1),
                            End = TimeSpan.FromSeconds(2),
                            Content = new Span[] { new Span() { Type = (SpanType)9876 } }
                        },
                    },
                    writer)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private class DerivedBlock : BaseBlock
        {
        }
    }
}
