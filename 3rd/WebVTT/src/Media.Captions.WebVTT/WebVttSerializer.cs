// ---------------------------------------------------------------------------
// <copyright file="WebVttSerializer.cs" owner="svm-git">
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

namespace Media.Captions.WebVTT
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Writes captions into a text in WebVTT format.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/ for more details.</remarks>
    public class WebVttSerializer
    {
        private const int MaxBufferSize = 32 * 1024;
        private static char[] newLine = new char[] { '\r', '\n' };
        private const string TimeSpanFormat = "d:hh:mm:ss.fff";

        /// <summary>
        /// Writes captions into a text in WebVTT format.
        /// </summary>
        /// <param name="captions">Caption blocks to serialize.</param>
        /// <param name="writer">Text writer to write into.</param>
        /// <remarks>See http://www.w3.org/TR/webvtt1/ for more details.</remarks>
        public static Task SerializeAsync(
            MediaCaptions captions,
            TextWriter writer)
        {
            return WebVttSerializer.SerializeAsync(
                WebVttSerializer.GetMediaCaptionBlocks(captions),
                writer);
        }

        /// <summary>
        /// Writes captions into a text in WebVTT format.
        /// </summary>
        /// <param name="captions">Caption blocks to serialize.</param>
        /// <param name="writer">Text writer to write into.</param>
        /// <remarks>See http://www.w3.org/TR/webvtt1/ for more details.</remarks>
        public static async Task SerializeAsync(
            IEnumerable<BaseBlock> captions,
            TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            StringBuilder builder = new StringBuilder(1024);

            builder.AppendLine(Constants.WebVttHeaderToken);

            if (captions != null)
            {
                Cue lastSeenCue = null;
                foreach (var block in captions)
                {
                    if (block == null)
                    {
                        throw new ArgumentException("Caption block cannot be null.", "captions");
                    }

                    Cue cue = block as Cue;

                    if (lastSeenCue != null)
                    {
                        if (block is RegionDefinition || block is Style)
                        {
                            throw new ArgumentException(
                                string.Format("{0} is not allowed after Cue.", block.GetType().Name),
                                "captions");
                        }

                        if (cue != null
                            && cue.Start < lastSeenCue.Start)
                        {
                            throw new ArgumentException(
                                string.Format("Cue start time '{0}' must be greater than or equal to previous cue start time '{1}'.",
                                    cue.Start.ToString("g"),
                                    lastSeenCue.Start.ToString("g")),
                                "captions");
                        }
                    }

                    if (cue != null)
                    {
                        lastSeenCue = cue;
                    }

                    WebVttSerializer.WriteBlock(block, builder);

                    if (builder.Length >= WebVttSerializer.MaxBufferSize)
                    {
                        await writer.WriteAsync(builder.ToString())
                            .ConfigureAwait(false);

                        builder.Clear();
                    }
                }
            }

            if (builder.Length > 0)
            {
                await writer.WriteAsync(builder.ToString())
                    .ConfigureAwait(false);
            }

            await writer.FlushAsync()
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Utility method to return all parts of media captions are a sequence of blocks.
        /// </summary>
        /// <param name="captions">Captions to process.</param>
        /// <returns>Sequence of blocks.</returns>
        private static IEnumerable<BaseBlock> GetMediaCaptionBlocks(
            MediaCaptions captions)
        {
            if (captions != null)
            {

                if (captions.Styles != null && captions.Styles.Length > 0)
                {
                    foreach (var style in captions.Styles)
                    {
                        if (style != null)
                        {
                            yield return style;
                        }
                    }
                }

                if (captions.Regions != null && captions.Regions.Length > 0)
                {
                    foreach (var region in captions.Regions)
                    {
                        if (region != null)
                        {
                            yield return region;
                        }
                    }
                }

                if (captions.Cues != null && captions.Cues.Length > 0)
                {
                    bool needSort = false;
                    Cue previous = null;

                    foreach (var cue in captions.Cues)
                    {
                        if (cue != null)
                        {
                            needSort = previous != null && cue.Start < previous.Start;
                            previous = cue;
                        }

                        if (needSort)
                        {
                            break;
                        }
                    }

                    if (needSort)
                    {
                        var tmp = new List<Cue>(captions.Cues.Length);
                        foreach (var cue in captions.Cues)
                        {
                            if (cue != null)
                            {
                                tmp.Add(cue);
                            }
                        }

                        if (tmp.Count > 0)
                        {
                            tmp.Sort((c1, c2) => c1.Start < c2.Start
                                ? -1
                                : (c1.Start > c2.Start ? 1 : c1.End.CompareTo(c2.End)));

                            foreach (var cue in tmp)
                            {
                                yield return cue;
                            }
                        }
                    }
                    else
                    {
                        foreach (var cue in captions.Cues)
                        {
                            yield return cue;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Utility method to write a single block into a buffer.
        /// </summary>
        /// <param name="block">Block to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WriteBlock(
            BaseBlock block,
            StringBuilder builder)
        {
            Cue cue = block as Cue;
            if (cue != null)
            {
                WebVttSerializer.WriteCue(cue, builder);
                return;
            }

            RegionDefinition region = block as RegionDefinition;
            if (region != null)
            {
                WebVttSerializer.WriteRegion(region, builder);
                return;
            }

            Style style = block as Style;
            if (style != null)
            {
                WebVttSerializer.WrteStyle(style, builder);
                return;
            }

            Comment comment = block as Comment;
            if (comment != null)
            {
                WebVttSerializer.WriteComment(comment, builder);
                return;
            }

            throw new ArgumentOutOfRangeException(
                string.Format("Unknown block type '{0}'.", block.GetType().FullName));
        }

        /// <summary>
        /// Utility method to write a comment block into a buffer.
        /// </summary>
        /// <param name="comment">Comment to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WriteComment(
            Comment comment,
            StringBuilder builder)
        {
            if (false == string.IsNullOrWhiteSpace(comment.RawContent))
            {
                if (comment.RawContent.Contains(Constants.ArrowToken))
                {
                    throw new ArgumentException(string.Format("Comment text cannot contain '{0}'.", Constants.ArrowToken));
                }

                builder.AppendLine();
                builder.Append(Constants.CommentToken);

                if (comment.RawContent.IndexOfAny(WebVttSerializer.newLine) >= 0)
                {
                    builder.AppendLine();
                    WebVttSerializer.WriteString(comment.RawContent.TrimEnd(WebVttSerializer.newLine), false, builder);
                }
                else
                {
                    builder.Append(" ");
                    builder.Append(comment.RawContent);
                }

                builder.AppendLine();
            }
        }

        /// <summary>
        /// Utility method to write style block into a buffer.
        /// </summary>
        /// <param name="style">Style block to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WrteStyle(
            Style style,
            StringBuilder builder)
        {
            if (false == string.IsNullOrWhiteSpace(style.RawContent))
            {
                if (style.RawContent.Contains(Constants.ArrowToken))
                {
                    throw new ArgumentException(string.Format("Style cannot contain '{0}'.", Constants.ArrowToken));
                }

                builder.AppendLine();
                builder.Append(Constants.StyleToken);

                if (style.RawContent.IndexOfAny(WebVttSerializer.newLine) >= 0)
                {
                    builder.AppendLine();
                    WebVttSerializer.WriteString(style.RawContent.TrimEnd(WebVttSerializer.newLine), false, builder);
                }
                else
                {
                    builder.Append(" ");
                    builder.Append(style.RawContent);
                }

                builder.AppendLine();
            }
        }

        /// <summary>
        /// Utility method to write region definition block into a buffer.
        /// </summary>
        /// <param name="region">Region definition to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WriteRegion(
            RegionDefinition region,
            StringBuilder builder)
        {
            builder.AppendLine();
            builder.AppendLine(Constants.RegionToken);

            if (false == string.IsNullOrWhiteSpace(region.Id))
            {
                builder.Append(Constants.RegionIdName).Append(":");
                WebVttSerializer.WriteString(region.Id, true, builder);
                builder.AppendLine();
            }

            if (region.Lines.HasValue
                && region.Lines.Value >= 0)
            {
                builder.Append(Constants.LinesName).Append(":").AppendLine(region.Lines.Value.ToString());
            }

            if (region.WidthPercent.HasValue)
            {
                builder
                    .Append(Constants.WidthName)
                    .Append(":")
                    .AppendLine(WebVttSerializer.GetPercentValue(region.WidthPercent.Value));
            }

            if (region.RegionAnchor.HasValue)
            {
                builder
                    .Append(Constants.RegionAnchorName).Append(":")
                    .Append(WebVttSerializer.GetPercentValue(region.RegionAnchor.Value.XPercent))
                    .Append(',')
                    .AppendLine(WebVttSerializer.GetPercentValue(region.RegionAnchor.Value.YPercent));
            }

            if (region.ViewPortAnchor.HasValue)
            {
                builder
                    .Append(Constants.ViewPortAnchorName).Append(":")
                    .Append(WebVttSerializer.GetPercentValue(region.ViewPortAnchor.Value.XPercent))
                    .Append(',')
                    .AppendLine(WebVttSerializer.GetPercentValue(region.ViewPortAnchor.Value.YPercent));
            }

            if (region.Scroll.HasValue && region.Scroll.Value)
            {
                builder.Append(Constants.ScrollName).Append(':').AppendLine(Constants.ScrollUpValue);
            }
        }

        /// <summary>
        /// Utility method to write a caption cue into a buffer.
        /// </summary>
        /// <param name="cue">Cue to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WriteCue(
            Cue cue,
            StringBuilder builder)
        {
            if (cue.End <= cue.Start)
            {
                throw new ArgumentException(string.Format("Cue start time '{0}' must be less than cue end time '{1}'.", cue.Start, cue.End));
            }

            builder.AppendLine();
            if (false == string.IsNullOrWhiteSpace(cue.Id))
            {
                builder.AppendLine(cue.Id);
            }

            WebVttSerializer.WriteTimeSpanValue(cue.Start, builder);
            builder
                .Append(' ')
                .Append(Constants.ArrowToken)
                .Append(' ');
            WebVttSerializer.WriteTimeSpanValue(cue.End, builder);

            if (false == string.IsNullOrWhiteSpace(cue.Region))
            {
                if (WebVttSerializer.HasWhiteSpace(cue.Region))
                {
                    throw new ArgumentException("White space characters are not allowed in cue region id.");
                }

                builder
                    .Append(' ')
                    .Append(Constants.RegionName).Append(':').Append(cue.Region);
            }

            if (cue.Alignment.HasValue)
            {
                builder
                    .Append(' ')
                    .Append(Constants.AlignName).Append(':').Append(WebVttSerializer.GetAlignmentValue(cue.Alignment.Value));
            }

            if (cue.Line.HasValue)
            {
                builder
                    .Append(' ')
                    .Append(Constants.LineName).Append(':');

                if (cue.Line.Value.Percent.HasValue)
                {
                    builder.Append(WebVttSerializer.GetPercentValue(cue.Line.Value.Percent.Value));
                }
                else if (cue.Line.Value.LineNumber.HasValue)
                {
                    builder.Append(cue.Line.Value.LineNumber.Value);
                }
                else
                {
                    throw new ArgumentException("Cue line setting must specify either percent or line number value.");
                }

                if (cue.Line.Value.Alignment.HasValue)
                {
                    builder.Append(',').Append(WebVttSerializer.GetLineAlignmentValue(cue.Line.Value.Alignment.Value));
                }
            }

            if (cue.Position.HasValue)
            {
                builder
                    .Append(' ')
                    .Append(Constants.PositionName).Append(':')
                    .Append(WebVttSerializer.GetPercentValue(
                        cue.Position.Value.PositionPercent.HasValue
                        ? cue.Position.Value.PositionPercent.Value
                        : 0.0));

                if (cue.Position.Value.Alignment.HasValue)
                {
                    builder
                        .Append(',')
                        .Append(WebVttSerializer.GetPositionAlignmentValue(cue.Position.Value.Alignment.Value));
                }
            }

            if (cue.SizePercent.HasValue)
            {
                builder
                    .Append(' ')
                    .Append(Constants.SizeName)
                    .Append(":")
                    .Append(WebVttSerializer.GetPercentValue(cue.SizePercent.Value));
            }

            if (cue.Vertical.HasValue)
            {
                builder
                    .Append(' ')
                    .Append(Constants.VerticalName)
                    .Append(":")
                    .Append(WebVttSerializer.GetVerticalAlignmentValue(cue.Vertical.Value));
            }

            builder.AppendLine();

            if (cue.Content != null && cue.Content.Length > 0)
            {
                Span previousSpan = null;
                foreach(var span in cue.Content)
                {
                    if (span == null)
                    {
                        throw new ArgumentException("Cue content cannot be null.");
                    }

                    if (WebVttSerializer.NeedNewLine(previousSpan, span))
                    {
                        builder.AppendLine();
                    }

                    WebVttSerializer.WriteSpan(span, builder);
                    previousSpan = span;
                }

                builder.AppendLine();
            }
        }

        /// <summary>
        /// Utility method to write a single cue span into a buffer.
        /// </summary>
        /// <param name="span">Cue span to write.</param>
        /// <param name="builder">String builder buffer to write into.</param>
        private static void WriteSpan(
            Span span,
            StringBuilder builder)
        {
            if (span.Type == SpanType.Text)
            {
                if (string.IsNullOrEmpty(span.Text))
                {
                    return;
                }

                if (span.Text.Contains(Constants.ArrowToken))
                {
                    throw new ArgumentException("Cue text cannot contain '{0}'.", Constants.ArrowToken);
                }

                WebVttSerializer.WriteString(span.Text, true, builder);
                return;
            }

            string tagName = WebVttSerializer.GetSpanTagName(span.Type);

            builder.Append('<').Append(tagName);

            if (span.Classes != null && span.Classes.Length > 0)
            {
                foreach (string cls in span.Classes)
                {
                    if (false == string.IsNullOrWhiteSpace(cls))
                    {
                        if (WebVttSerializer.HasWhiteSpace(cls))
                        {
                            throw new ArgumentException("White space characters are not allowed in span class name.");
                        }

                        builder.Append('.').Append(cls);
                    }
                }
            }

            if (false == string.IsNullOrWhiteSpace(span.Annotation))
            {
                builder.Append(' ');
                WebVttSerializer.WriteString(span.Annotation, true, builder);
            }

            builder.Append('>');

            if (span.Children != null && span.Children.Length > 0)
            {
                foreach (var child in span.Children)
                {
                    if (child != null)
                    {
                        WebVttSerializer.WriteSpan(child, builder);
                    }
                }
            }

            builder.Append("</").Append(tagName).Append('>');
        }

        /// <summary>
        /// Utility method to check if need to start new line for the next span.
        /// </summary>
        /// <param name="previousSpan">Previous span.</param>
        /// <param name="span">Next span.</param>
        /// <returns>True if next span should be written on the next line.</returns>
        private static bool NeedNewLine(
            Span previousSpan,
            Span span)
        {
            if (previousSpan == null)
            {
                return false;
            }

            if (span.Type == SpanType.Voice
                || span.Type == SpanType.Ruby)
            {
                return true;
            }

            if (span.Type == SpanType.Text && previousSpan.Type == SpanType.Text)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Utility method to serialize percent value.
        /// </summary>
        /// <param name="percent">Percent value to serialize.</param>
        /// <returns>Serialized value.</returns>
        private static string GetPercentValue(
            double percent)
        {
            if (percent < 0)
            {
                return "0%";
            }

            if (percent > 100.0)
            {
                return "100%";
            }

            return percent.ToString() + "%";
        }

        /// <summary>
        /// Utility method to serialize elements of TextAlignment enum.
        /// </summary>
        /// <param name="textAlignment">Enum value to serialize.</param>
        /// <returns>Serialized enum value.</returns>
        private static string GetAlignmentValue(
            TextAlignment textAlignment)
        {
            switch (textAlignment)
            {
                case TextAlignment.Start:
                    return Constants.StartValue;

                case TextAlignment.Center:
                    return Constants.CenterValue;

                case TextAlignment.End:
                    return Constants.EndValue;

                case TextAlignment.Left:
                    return Constants.LeftValue;

                case TextAlignment.Right:
                    return Constants.RightValue;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown cue align value '{0}'.", textAlignment.ToString()));
            }
        }

        /// <summary>
        /// Utility method to serialize elements of LineAlignment enum.
        /// </summary>
        /// <param name="lineAlignment">Enum value to serialize.</param>
        /// <returns>Serialized enum value.</returns>
        private static string GetLineAlignmentValue(LineAlignment lineAlignment)
        {
            switch (lineAlignment)
            {
                case LineAlignment.Start:
                    return Constants.StartValue;

                case LineAlignment.Center:
                    return Constants.CenterValue;

                case LineAlignment.End:
                    return Constants.EndValue;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown cue line align value '{0}'.", lineAlignment.ToString()));
            }
        }

        /// <summary>
        /// Utility method to serialize elements of PositionAlignment enum.
        /// </summary>
        /// <param name="positionAlignment">Enum value to serialize.</param>
        /// <returns>Serialized enum value.</returns>
        private static string GetPositionAlignmentValue(
            PositionAlignment positionAlignment)
        {
            switch (positionAlignment)
            {
                case PositionAlignment.LineLeft:
                    return Constants.LineLeftValue;

                case PositionAlignment.Center:
                    return Constants.CenterValue;

                case PositionAlignment.LineRight:
                    return Constants.LineRightValue;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown cue position align value '{0}'.", positionAlignment.ToString()));
            }
        }

        /// <summary>
        /// Utility method to serialize elements of VerticalTextLayout enum.
        /// </summary>
        /// <param name="verticalTextLayout">Enum value to serialize.</param>
        /// <returns>Serialized enum value.</returns>
        private static string GetVerticalAlignmentValue(
            VerticalTextLayout verticalTextLayout)
        {
            switch (verticalTextLayout)
            {
                case VerticalTextLayout.RightToLeft:
                    return Constants.RightToLeftValue;

                case VerticalTextLayout.LeftToRight:
                    return Constants.LeftToRightValue;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown cue vertical align value '{0}'.", verticalTextLayout.ToString()));
            }
        }

        private static void WriteTimeSpanValue(
            TimeSpan timespan,
            StringBuilder builder)
        {
            if (timespan.Days > 0)
            {
                builder.Append(timespan.Days).Append(':');
            }

            builder
                .Append(timespan.Hours.ToString("d02"))
                .Append(':')
                .Append(timespan.Minutes.ToString("d02"))
                .Append(':')
                .Append(timespan.Seconds.ToString("d02"))
                .Append('.')
                .Append(timespan.Milliseconds.ToString("d03"));
        }

        /// <summary>
        /// Utility method to get span tag name from its type.
        /// </summary>
        /// <param name="spanType">Span type.</param>
        /// <returns>Tag name for the span.</returns>
        private static string GetSpanTagName(
            SpanType spanType)
        {
            switch (spanType)
            {
                case SpanType.Class:
                    return Constants.ClassSpanName;

                case SpanType.Italics:
                    return Constants.ItalicsSpanName;

                case SpanType.Bold:
                    return Constants.BoldSpanName;

                case SpanType.Underline:
                    return Constants.UnderlineSpanName;

                case SpanType.Ruby:
                    return Constants.RubySpanName;

                case SpanType.RubyText:
                    return Constants.RubyTextSpanName;

                case SpanType.Voice:
                    return Constants.VoiceSpanName;

                case SpanType.Language:
                    return Constants.LanguageSpanName;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("Unknown cue span type '{0}'.", spanType.ToString()));
            }
        }

        /// <summary>
        /// Checks if string has white space characters.
        /// </summary>
        /// <param name="input">Input string to test.</param>
        /// <returns>True if string has white space characters; otherwise false.</returns>
        private static bool HasWhiteSpace(string input)
        {
            if (input == null || input.Length == 0)
            {
                return false;
            }

            foreach (char c in input)
            {
                if (char.IsWhiteSpace(c))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Utility method to write sanitized string value.
        /// </summary>
        /// <param name="input">Input string to write.</param>
        /// <param name="removeNewLines">If true, replaces consequetive new lines with a space.
        /// If false, replaces consecutive new lines with a single new line.</param>
        /// <param name="builder">String buffer to write into.</param>
        private static void WriteString(
            string input,
            bool removeNewLines,
            StringBuilder builder)
        {
            if (false == string.IsNullOrEmpty(input))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    for (; i < input.Length && input[i] != '\r' && input[i] != '\n'; i++)
                    {
                        builder.Append(input[i]);
                    }

                    if (i < input.Length)
                    {
                        for (; i < input.Length && (input[i] == '\r' || input[i] == '\n'); i++)
                        {
                        }

                        if (removeNewLines)
                        {
                            builder.Append(' ');
                        }
                        else
                        {
                            builder.AppendLine();
                        }

                        if (i < input.Length)
                        {
                            builder.Append(input[i]);
                        }
                    }
                }
            }
        }
    }
}
