// ---------------------------------------------------------------------------
// <copyright file="WebVttParser.cs" owner="svm-git">
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
    /// Parses a text in WebVTT format.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/ for more details.</remarks>
    public static class WebVttParser
    {
        /// <summary>
        /// Reads WebVTT media captions from a given <see cref="TextReader"/>.
        /// </summary>
        /// <param name="reader">Text reader to read the captions.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        public static async Task<MediaCaptions> ReadMediaCaptionsAsync(
            TextReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            // Process stream header line
            var line = await reader.ReadLineAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(line) 
                || line.Length < 6
                || false == line.StartsWith(Constants.WebVttHeaderToken)
                || line.Length >= 7 && line[6] != '\t' && line[6] != ' ')
            {
                throw new InvalidDataException("The stream does not start with the correct WebVTT file signature.");
            }

            var result = new MediaCaptions();

            // Process (skip over) optional headers from the stream
            while (false == string.IsNullOrEmpty(line))
            {
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);
            }

            // Process media caption blocks.
            List<RegionDefinition> regions = new List<RegionDefinition>();
            List<Style> styles = new List<Style>();
            List<Cue> cues = new List<Cue>();
            
            BaseBlock block;

            do
            {
                block = await WebVttParser.ReadBlockAsync(reader)
                    .ConfigureAwait(false);

                RegionDefinition region = block as RegionDefinition;
                Style style = block as Style;
                Cue cue = block as Cue;

                if (cues.Count == 0)
                {
                    if (region != null)
                    {
                        regions.Add(region);
                    }
                    else if (style != null)
                    {
                        styles.Add(style);
                    }
                }
                else if (region != null || style != null)
                {
                    throw new InvalidDataException("Region or style blocks cannot be mixed with cue blocks.");
                }

                if (cue != null)
                {
                    cues.Add(cue);
                }
            }
            while (block != null);

            if (regions.Count > 0)
            {
                result.Regions = regions.ToArray();
            }

            if (styles.Count > 0)
            {
                result.Styles = styles.ToArray();
            }

            if (cues.Count > 0)
            {
                result.Cues = cues.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Reads WebVTT block from the reader.
        /// </summary>
        /// <param name="reader">Text reader to read the region block.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private static async Task<BaseBlock> ReadBlockAsync(
            TextReader reader)
        {
            var line = await reader.ReadLineAsync()
                .ConfigureAwait(false);

            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            if (line.StartsWith(Constants.RegionToken))
            {
                return await WebVttParser.ReadRegionAsync(line, reader)
                    .ConfigureAwait(false);
            }

            if (line.StartsWith(Constants.StyleToken))
            {
                return await WebVttParser.ReadStyleAsync(line, reader)
                    .ConfigureAwait(false);
            }

            if (line.StartsWith(Constants.CommentToken))
            {
                return await WebVttParser.ReadCommentAsync(line, reader)
                    .ConfigureAwait(false);
            }

            return await ReadCueAsync(line, reader)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Reads WebVTT Region definition block from the stream.
        /// </summary>
        /// <param name="firstLine">First line of the block read from the reader.</param>
        /// <param name="reader">Text reader to read the region block.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private static async Task<RegionDefinition> ReadRegionAsync(
            string firstLine,
            TextReader reader)
        {
            string line = firstLine;
            if (line.Length > Constants.RegionToken.Length
                && false == string.IsNullOrWhiteSpace(line.Substring(Constants.RegionToken.Length)))
            {
                throw new InvalidDataException(string.Format("Invalid characters found after region definition header: {0}", line));
            }

            var content = new StringBuilder(100);

            while (false == string.IsNullOrEmpty(line))
            {
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);

                if (false == string.IsNullOrEmpty(line))
                {
                    if (line.Contains(Constants.ArrowToken))
                    {
                        throw new InvalidDataException(string.Format("Region definition must not contain '{0}'.", Constants.ArrowToken));
                    }

                    content.SafeAppendLine(line);
                }
            }

            var result = new RegionDefinition()
            {
                RawContent = content.Length > 0 ? content.ToString() : null
            };

            if (result.RawContent != null)
            {
                var settings = WebVttParser.ParseSettings(result.RawContent);

                if (settings != null)
                {
                    string value;
                    if (WebVttParser.TryGetStringSetting(Constants.RegionIdName, settings, out value))
                    {
                        result.Id = value;
                    }

                    double percent;
                    if (WebVttParser.TryGetPercentSetting(Constants.WidthName, settings, out percent))
                    {
                        result.WidthPercent = percent;
                    }

                    Anchor anchor;
                    if (WebVttParser.TryGetAnchorSetting(Constants.RegionAnchorName, settings, out anchor))
                    {
                        result.RegionAnchor = anchor;
                    }

                    if (WebVttParser.TryGetAnchorSetting(Constants.ViewPortAnchorName, settings, out anchor))
                    {
                        result.ViewPortAnchor = anchor;
                    }

                    if (WebVttParser.TryGetStringSetting(Constants.ScrollName, settings, out value))
                    {
                        result.Scroll = string.Equals(value, Constants.ScrollUpValue, StringComparison.OrdinalIgnoreCase);
                    }

                    int lines;
                    if (WebVttParser.TryGetIntSetting(Constants.LinesName, settings, out lines))
                    {
                        result.Lines = lines;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Reads WebVTT Style block from the reader.
        /// </summary>
        /// <param name="firstLine">First line of the block read from the stream.</param>
        /// <param name="reader">Text reader to read the style block.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private static async Task<Style> ReadStyleAsync(
            string firstLine,
            TextReader reader)
        {
            string line = firstLine;
            if (line.Length > Constants.StyleToken.Length
                && false == string.IsNullOrWhiteSpace(line.Substring(Constants.StyleToken.Length)))
            {
                throw new InvalidDataException(string.Format("Invalid characters found after style header: {0}", line));
            }

            var content = new StringBuilder(100);

            while (false == string.IsNullOrEmpty(line))
            {
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);

                if (false == string.IsNullOrEmpty(line))
                {
                    if (line.Contains(Constants.ArrowToken))
                    {
                        throw new InvalidDataException(string.Format("Style definition must not contain '{0}'.", Constants.ArrowToken));
                    }

                    content.SafeAppendLine(line);
                }
            }

            return new Style() 
            {
                RawContent = content.Length > 0 ? content.ToString() : null
            };
        }

        /// <summary>
        /// Reads WebVTT Comment block from the reader.
        /// </summary>
        /// <param name="firstLine">First line of the block read from the stream.</param>
        /// <param name="reader">Text reader to read the style block.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private static async Task<Comment> ReadCommentAsync(
            string firstLine,
            TextReader reader)
        {
            string line = firstLine;
            var content = new StringBuilder(100);

            if (line.Length > Constants.CommentToken.Length)
            {
                var inlineComment = line.Substring(Constants.CommentToken.Length).TrimStart('\t', ' ');
                if (false == string.IsNullOrWhiteSpace(inlineComment))
                {
                    content.Append(inlineComment);
                }
            }

            while (false == string.IsNullOrEmpty(line))
            {
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);

                if (false == string.IsNullOrEmpty(line))
                {
                    content.SafeAppendLine(line);
                }
            }

            return new Comment()
            {
                RawContent = content.Length > 0 ? content.ToString() : null
            };
        }

        /// <summary>
        /// Reads WebVTT Cue block from the reader.
        /// </summary>
        /// <param name="firstLine">First line of the block read from the reader.</param>
        /// <param name="reader">Text reader to read the style block.</param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private static async Task<Cue> ReadCueAsync(
            string firstLine,
            TextReader reader)
        {
            string line = firstLine;
            var content = new StringBuilder(100);

            Cue result = new Cue();

            if (false == line.Contains(Constants.ArrowToken))
            {
                result.Id = line;
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);
            }

            int position = 0;
            result.Start = ParseTimeSpan(line, ref position);

            while (position < line.Length && char.IsWhiteSpace(line, position))
            {
                position++;
            }

            if (0 != string.CompareOrdinal(line, position, Constants.ArrowToken, 0, Constants.ArrowToken.Length))
            {
                throw new InvalidDataException(string.Format("Invalid characters found in cue timings '{0}' at position {1}.", line, position));
            }

            position += Constants.ArrowToken.Length;

            while (position < line.Length && char.IsWhiteSpace(line, position))
            {
                position++;
            }

            result.End = ParseTimeSpan(line, ref position);

            if (result.End < result.Start)
            {
                throw new InvalidDataException(string.Format("Cue start time is greater than end time in '{0}'.", line));
            }

            if (position < line.Length)
            {
                var settings = line.Substring(position).TrimStart('\t', ' ').TrimEnd('\t', ' ');
                if (false == string.IsNullOrWhiteSpace(settings))
                {
                    result.RawSettings = settings;

                    var parsedSettings = WebVttParser.ParseSettings(settings);
                    if (parsedSettings != null)
                    {
                        VerticalTextLayout vertical;
                        if (WebVttParser.TryGetVerticalTextLayoutSetting(Constants.VerticalName, parsedSettings, out vertical))
                        {
                            result.Vertical = vertical;
                        }

                        LineSettings lineSetting;
                        if (WebVttParser.TryGetLineSetting(Constants.LineName, parsedSettings, out lineSetting))
                        {
                            result.Line = lineSetting;
                        }

                        PositionSettings positionSetting;
                        if (WebVttParser.TryGetPositionSetting(Constants.PositionName, parsedSettings, out positionSetting))
                        {
                            result.Position = positionSetting;
                        }

                        double percent;
                        if (WebVttParser.TryGetPercentSetting(Constants.SizeName, parsedSettings, out percent))
                        {
                            result.SizePercent = percent;
                        }

                        TextAlignment alignment;
                        if (WebVttParser.TryGetAlignmentSetting(Constants.AlignName, parsedSettings, out alignment))
                        {
                            result.Alignment = alignment;
                        }

                        string name;
                        if (WebVttParser.TryGetStringSetting(Constants.RegionName, parsedSettings, out name))
                        {
                            result.Region = name;
                        }
                    }
                }
            }

            while (false == string.IsNullOrEmpty(line))
            {
                line = await reader.ReadLineAsync()
                    .ConfigureAwait(false);

                if (false == string.IsNullOrEmpty(line))
                {
                    if (line.Contains(Constants.ArrowToken))
                    {
                        throw new InvalidDataException(string.Format("Cue must not contain '{0}'.", Constants.ArrowToken));
                    }

                    content.SafeAppendLine(line);
                }
            }

            if (content.Length > 0)
            {
                result.RawContent = content.ToString();

                List<Span> spans;

                position = 0;
                if (WebVttParser.TryParseSpans(result.RawContent, ref position, out spans)
                    && spans != null
                    && spans.Count > 0)
                {
                    result.Content = spans.ToArray();
                }
            }

            return result;
        }

        /// <summary>
        /// Parses time span value from cue timing string.
        /// </summary>
        /// <param name="line">String to process.</param>
        /// <param name="position">Current position in the line.</param>
        /// <returns>TimeSpan read from the string.</returns>
        private static TimeSpan ParseTimeSpan(string line, ref int position)
        {
            int initialPosition = position;

            int[] values = new int[4];
            int[] valueDigits = new int[4];

            int valueIndex = 0;
            
            while (position < line.Length)
            {
                char c = line[position]; 
                if (char.IsDigit(c))
                {
                    values[valueIndex] = values[valueIndex] * 10 + (c - '0');
                    valueDigits[valueIndex]++;

                    if (valueIndex >= 2 && valueDigits[valueIndex] > 3
                        || valueIndex > 0 && valueIndex < 2 && valueDigits[valueIndex] > 2)
                    {
                        throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                    }
                }
                else if (c == '.')
                {
                    if (valueIndex < 1
                        || valueDigits[valueIndex] != 2)
                    {
                        throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                    }

                    valueIndex++;
                }
                else if (c == ':')
                {
                    if (valueIndex > 0
                        && valueDigits[valueIndex] != 2)
                    {
                        throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                    }

                    valueIndex++;
                }
                else if (char.IsWhiteSpace(c) || c == '-')
                {
                    if (valueDigits[valueIndex] != 3)
                    {
                        throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                    }

                    break;
                }
                else
                {
                    throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                }

                if (valueIndex >= values.Length)
                {
                    throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
                }

                position++;
            }

            if (valueIndex < 2)
            {
                throw new InvalidDataException(string.Format("Invalid character in cue timing value at position {0} in '{1}'.", position, line));
            }

            try
            {
                if (valueIndex == 2)
                {
                    if (values[0] > 59 || values[1] > 59
                        || valueDigits[0] != 2 || valueDigits[1] != 2 || valueDigits[2] != 3)
                    {
                        throw new InvalidDataException(string.Format("Invalid cue timing value '{0}'.", line));
                    }

                    return new TimeSpan(0, 0, values[0], values[1], values[2]);
                }

                if (values[1] > 59 || values[2] > 59
                    || valueDigits[1] != 2 || valueDigits[2] != 2 || valueDigits[3] != 3)
                {
                    throw new InvalidDataException(string.Format("Invalid cue timing value '{0}'.", line));
                }

                return new TimeSpan(values[0] / 24, values[0] % 24, values[1], values[2], values[3]);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new InvalidDataException(string.Format("Cue timing value '{0}' is too large.", line));
            }
        }

        /// <summary>
        /// Utility method to parse region or cue setting values.
        /// </summary>
        /// <param name="rawSettings">Raw settings string to parse.</param>
        /// <returns>Collection of setting values parsed from the input.</returns>
        private static Dictionary<string, string> ParseSettings(
            string rawSettings)
        {
            Dictionary<string, string> result = null;

            if (false == string.IsNullOrWhiteSpace(rawSettings))
            {
                int start = 0;
                int current = 0;
                foreach (char c in rawSettings)
                {
                    bool last = rawSettings.Length - current == 1;
                    if (char.IsWhiteSpace(c) || last)
                    {
                        if (start < current || last)
                        {
                            KeyValuePair<string, string> setting;
                            if (WebVttParser.TryParseSettingValue(rawSettings.Substring(start, current - start + (last ? 1 : 0)), out setting))
                            {
                                if (result == null)
                                {
                                    result = new Dictionary<string, string>(5, StringComparer.OrdinalIgnoreCase);
                                }

                                result[setting.Key] = setting.Value;
                            }
                            else
                            {
                                throw new InvalidDataException(string.Format("Invalid setting value in '{0}'.", rawSettings));
                            }
                        }

                        current++;
                        start = current;
                    }
                    else
                    {
                        current++;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Utility method to parse setting value pair.
        /// </summary>
        /// <param name="input">Input string to parse.</param>
        /// <param name="setting">If parsing is successfull, contains setting name and value.</param>
        /// <returns>True if setting value was parsed successfully; otherwise false.</returns>
        private static bool TryParseSettingValue(
            string input,
            out KeyValuePair<string, string> setting)
        {
            setting = default(KeyValuePair<string, string>);

            int index = input.IndexOf(':');
            if (index < 0 || input.Length - index < 2)
            {
                return false;
            }

            setting = new KeyValuePair<string,string>(
                input.Substring(0, index),
                input.Substring(index + 1, input.Length - index - 1));

            return true;
        }

        /// <summary>
        /// Utility method to get string setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise null.</param>
        /// <returns>True if the setting exists and is not empty; otherwise false.</returns>
        private static bool TryGetStringSetting(
            string name,
            Dictionary<string, string> settings,
            out string value)
        {
            if (settings.TryGetValue(name, out value)
                && false == string.IsNullOrEmpty(value))
            {
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Utility method to get percent setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetPercentSetting(
            string name,
            Dictionary<string, string> settings,
            out double value)
        {
            string stringValue;
            if (settings.TryGetValue(name, out stringValue)
                && false == string.IsNullOrEmpty(stringValue))
            {
                return WebVttParser.TryParsePercent(stringValue, out value);
            }

            value = default(double);
            return false;
        }

        /// <summary>
        /// Utility method to get anchor setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetAnchorSetting(
            string name,
            Dictionary<string, string> settings,
            out Anchor value)
        {
            value = default(Anchor);

            string stringValue;
            if (false == settings.TryGetValue(name, out stringValue)
                || string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            int index = stringValue.IndexOf(',');
            if (index < 2 || stringValue.Length - index < 3)
            {
                return false;
            }

            if (false == WebVttParser.TryParsePercent(stringValue.Substring(0, index), out value.XPercent)
                || false == WebVttParser.TryParsePercent(stringValue.Substring(index + 1, stringValue.Length - index - 1), out value.YPercent))
            {
                value = default(Anchor);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Utility method to get integer setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetIntSetting(
            string name,
            Dictionary<string, string> settings,
            out int value)
        {
            string stringValue;
            if (settings.TryGetValue(name, out stringValue)
                && false == string.IsNullOrEmpty(stringValue))
            {
                return int.TryParse(stringValue, out value);
            }

            value = default(int);
            return false;
        }

        /// <summary>
        /// Utility method to get vertical text layout setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetVerticalTextLayoutSetting(
            string name,
            Dictionary<string, string> settings,
            out VerticalTextLayout value)
        {
            string stringValue;
            if (settings.TryGetValue(name, out stringValue)
                && false == string.IsNullOrEmpty(stringValue))
            {
                if (string.Equals(Constants.RightToLeftValue, stringValue, StringComparison.OrdinalIgnoreCase))
                {
                    value = VerticalTextLayout.RightToLeft;
                    return true;
                }

                if (string.Equals(Constants.LeftToRightValue, stringValue, StringComparison.OrdinalIgnoreCase))
                {
                    value = VerticalTextLayout.LeftToRight;
                    return true;
                }
            }

            value = default(VerticalTextLayout);
            return false;
        }

        /// <summary>
        /// Utility method to get line setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetLineSetting(
            string name,
            Dictionary<string, string> settings,
            out LineSettings value)
        {
            value = default(LineSettings);

            string stringValue;
            if (false == settings.TryGetValue(name, out stringValue)
                || string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            string offsetValue = stringValue;
            int commaIndex = stringValue.IndexOf(',');
            if (0 <= commaIndex)
            {
                if (stringValue.Length - commaIndex < 2)
                {
                    return false;
                }

                string alignment = stringValue.Substring(commaIndex + 1, stringValue.Length - commaIndex - 1);
                if (string.Equals(Constants.StartValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = LineAlignment.Start;
                }
                else if (string.Equals(Constants.CenterValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = LineAlignment.Center;
                }
                else if (string.Equals(Constants.EndValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = LineAlignment.End;
                }
                else
                {
                    return false;
                }

                offsetValue = stringValue.Substring(0, commaIndex);
            }

            double percent;
            if (WebVttParser.TryParsePercent(offsetValue, out percent))
            {
                value.Percent = percent;
                return true;
            }

            int number;
            if (int.TryParse(offsetValue, out number))
            {
                value.LineNumber = number;
                return true;
            }

            value = default(LineSettings);
            return false;
        }

        /// <summary>
        /// Utility method to get position setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetPositionSetting(
            string name,
            Dictionary<string, string> settings,
            out PositionSettings value)
        {
            value = default(PositionSettings);

            string stringValue;
            if (false == settings.TryGetValue(name, out stringValue)
                || string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            string positionValue = stringValue;
            int commaIndex = stringValue.IndexOf(',');
            if (0 <= commaIndex)
            {
                if (stringValue.Length - commaIndex < 2)
                {
                    return false;
                }

                string alignment = stringValue.Substring(commaIndex + 1, stringValue.Length - commaIndex - 1);
                if (string.Equals(Constants.LineLeftValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = PositionAlignment.LineLeft;
                }
                else if (string.Equals(Constants.CenterValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = PositionAlignment.Center;
                }
                else if (string.Equals(Constants.LineRightValue, alignment, StringComparison.OrdinalIgnoreCase))
                {
                    value.Alignment = PositionAlignment.LineRight;
                }
                else
                {
                    return false;
                }

                positionValue = stringValue.Substring(0, commaIndex);
            }

            double percent;
            if (WebVttParser.TryParsePercent(positionValue, out percent))
            {
                value.PositionPercent = percent;
                return true;
            }

            value = default(PositionSettings);
            return false;
        }

        /// <summary>
        /// Utility method to get alignment setting value.
        /// </summary>
        /// <param name="name">Setting name.</param>
        /// <param name="settings">Setting property bag.</param>
        /// <param name="value">If successful, contains setting value; otherwise default value.</param>
        /// <returns>True if the setting exists and is valid; otherwise false.</returns>
        private static bool TryGetAlignmentSetting(
            string name,
            Dictionary<string, string> settings,
            out TextAlignment value)
        {
            value = default(TextAlignment);

            string stringValue;
            if (false == settings.TryGetValue(name, out stringValue)
                || string.IsNullOrEmpty(stringValue))
            {
                return false;
            }

            if (string.Equals(Constants.StartValue, stringValue, StringComparison.OrdinalIgnoreCase))
            {
                value = TextAlignment.Start;
            }
            else if (string.Equals(Constants.CenterValue, stringValue, StringComparison.OrdinalIgnoreCase))
            {
                value = TextAlignment.Center;
            }
            else if (string.Equals(Constants.EndValue, stringValue, StringComparison.OrdinalIgnoreCase))
            {
                value = TextAlignment.End;
            }
            else if (string.Equals(Constants.LeftValue, stringValue, StringComparison.OrdinalIgnoreCase))
            {
                value = TextAlignment.Left;
            }
            else if (string.Equals(Constants.RightValue, stringValue, StringComparison.OrdinalIgnoreCase))
            {
                value = TextAlignment.Right;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Utility method to parse percent value from string.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <param name="value">If successful, percent value read from the input; otherwise default.</param>
        /// <returns>True if the percent value was read successfully; otherwise false.</returns>
        private static bool TryParsePercent(
            string input,
            out double value)
        {
            value = default(double);
            if (input.Length < 2 || input[input.Length - 1] != '%')
            {
                return false;
            }

            if (false == double.TryParse(input.Substring(0, input.Length - 1), out value))
            {
                return false;
            }

            if (value < 0.0 || value > 100.0)
            {
                value = default(double);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Utility method to parse cue content and create a span syntax tree.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <param name="position">Position in the string to begin parsing.</param>
        /// <param name="spans">If successful, contains the list of spans parsed from the input.</param>
        /// <returns>True if successfully parsed spans from the input; otherwise false.</returns>
        private static bool TryParseSpans(
            string input,
            ref int position,
            out List<Span> spans)
        {
            spans = null;

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }

            List<Span> result = null;
            int textStart = position;

            for (; position < input.Length; position++)
            {
                char c = input[position];
                bool last = position == input.Length - 1;

                if (c == '<' || last || c == '\r' || c == '\n')
                {
                    if (textStart < position || last && c != '<')
                    {
                        WebVttParser.SafeAddSpan(
                            new Span() 
                            {
                                Type = SpanType.Text,
                                Text = input.Substring(textStart, position - textStart + (last ? 1 : 0))
                            },
                            ref result);
                    }

                    if (false == last)
                    {
                        if (c == '<' && input[position + 1] == '/')
                        {
                            break;
                        }

                        if (c == '<')
                        {
                            Span span;
                            if (false == WebVttParser.TryParseSpan(input, ref position, out span))
                            {
                                return false;
                            }

                            WebVttParser.SafeAddSpan(span, ref result);
                        }

                        textStart = position + 1;
                    }
                    else if (c == '<')
                    {
                        return false;
                    }
                }
            }

            spans = result;
            return true;
        }

        /// <summary>
        /// Utility method to parse single span from text input.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <param name="position">Position in the string to begin parsing.</param>
        /// <param name="span">If successful, contains span parsed from the input.</param>
        /// <returns>True if span was successfully parsed from the input; otherwise false.</returns>
        private static bool TryParseSpan(
            string input,
            ref int position,
            out Span span)
        {
            span = null;

            if (position >= input.Length || input[position] != '<')
            {
                return false;
            }

            string tagName;
            string endTagName;
            string annotation;
            List<string> classes;
            List<Span> innerSpans;

            if (false == WebVttParser.TryParseTagStart(input, ref position, out tagName, out classes, out annotation))
            {
                return false;
            }

            if (false == WebVttParser.TryParseSpans(input, ref position, out innerSpans))
            {
                return false;
            }

            if (false == WebVttParser.TryParseTagEnd(input, ref position, out endTagName))
            {
                return false;
            }

            if (endTagName != null && false == string.Equals(tagName, endTagName, StringComparison.Ordinal))
            {
                return false;
            }

            SpanType spanType;
            if (false == WebVttParser.TryGetSpanTypeFromName(tagName, out spanType))
            {
                return false;
            }

            span = new Span()
            {
                Type = spanType,
                Children = innerSpans != null && innerSpans.Count > 0 ? innerSpans.ToArray() : null,
                Annotation = annotation,
                Classes = classes != null && classes.Count > 0 ? classes.ToArray() : null
            };

            return true;
        }

        /// <summary>
        /// Utility method to get span type by the tag name.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="spanType">If successful, type of the span for this name.</param>
        /// <returns>True if the tag name matches one of the cue spans; otherwise false.</returns>
        private static bool TryGetSpanTypeFromName(
            string tagName,
            out SpanType spanType)
        {
            if (string.Equals(tagName, Constants.ClassSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Class;
                return true;
            }

            if (string.Equals(tagName, Constants.ItalicsSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Italics;
                return true;
            }

            if (string.Equals(tagName, Constants.BoldSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Bold;
                return true;
            }

            if (string.Equals(tagName, Constants.LanguageSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Language;
                return true;
            }

            if (string.Equals(tagName, Constants.RubySpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Ruby;
                return true;
            }

            if (string.Equals(tagName, Constants.RubyTextSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.RubyText;
                return true;
            }

            if (string.Equals(tagName, Constants.UnderlineSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Underline;
                return true;
            }

            if (string.Equals(tagName, Constants.VoiceSpanName, StringComparison.Ordinal))
            {
                spanType = SpanType.Voice;
                return true;
            }

            spanType = default(SpanType);
            return false;
        }

        /// <summary>
        /// Utility method to parse start tag from the input string.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <param name="position">Position in the string to begin parsing.</param>
        /// <param name="tagName">If successfull, contains the name of the tag; otherwise null.</param>
        /// <param name="classes">If successfull, contains list of classes for the tag; otherwise null.</param>
        /// <param name="annotation">If successfull, contains the annotation text of the tag; otherwise null.</param>
        /// <returns>True if parsing was successful; otherwise false.</returns>
        private static bool TryParseTagStart(
            string input,
            ref int position,
            out string tagName,
            out List<string> classes,
            out string annotation)
        {
            tagName = null;
            classes = null;
            annotation = null;

            if (position >= input.Length
                || input.Length - position < 3
                || input[position] != '<')
            {
                return false;
            }

            position++;
            int start = position;
            for (; position < input.Length; position++)
            {
                char c = input[position];
                if (char.IsWhiteSpace(c)
                    || c == '.'
                    || c == '>')
                {
                    break;
                }
            }

            if (input.Length <= position || start == position)
            {
                return false;
            }

            switch (input[position])
            {
                case '\r':
                case '\n':
                    return false;
            }

            tagName = input.Substring(start, position - start);

            if (input[position] == '.')
            {
                position++;
                start = position;
                for (; position < input.Length; position++)
                {
                    char c = input[position];
                    if (c == '.' || char.IsWhiteSpace(c) || c == '>')
                    {
                        if (start == position)
                        {
                            return false;
                        }

                        if (classes == null)
                        {
                            classes = new List<string>();
                        }

                        classes.Add(input.Substring(start, position - start));
                        start = position + 1;

                        if (c != '.')
                        {
                            break;
                        }
                    }
                }

                if (input.Length <= position)
                {
                    return false;
                }
            }

            if (input[position] == '>')
            {
                position++;
                return true;
            }

            position++;
            start = position;

            while (position < input.Length && input[position] != '>' && input[position] != '\r' && input[position] != '\n')
            {
                position++;
            }

            if (input.Length <= position
                || input[position] != '>')
            {
                return false;
            }

            annotation = input.Substring(start, position - start);
            position++;
            return true;
        }

        /// <summary>
        /// Utility method to parse tag end marker.
        /// </summary>
        /// <param name="input">Input string to process.</param>
        /// <param name="position">Position in the string to begin parsing.</param>
        /// <param name="tagName">If successful, contains tag name of the end tag marker.</param>
        /// <returns>True if tag end was successfully parsed; otherwise false.</returns>
        private static bool TryParseTagEnd(
            string input,
            ref int position,
            out string tagName)
        {
            tagName = null;

            if (input.Length == position)
            {
                return true;
            }

            int start = position;
            char c = input[position];
            while ((c == '\r' || c == '\n') && position < input.Length)
            {
                position++;
                c = input[position];
            }

            if (start < position)
            {
                return true;
            }

            if (input.Length - position < 4 || c != '<')
            {
                return false;
            }

            position++;
            if (input[position] != '/')
            {
                return false;
            }

            position++;
            start = position;
            for (; position < input.Length; position++)
            {
                c = input[position];
                if (c == '>')
                {
                    break;
                }
            }

            if (c != '>')
            {
                return false;
            }

            tagName = input.Substring(start, position - start);
            return true;
        }

        /// <summary>
        /// Utility method to add a span to 
        /// </summary>
        /// <param name="span"></param>
        /// <param name="spans"></param>
        private static void SafeAddSpan(
            Span span,
            ref List<Span> spans)
        {
            if (spans == null)
            {
                spans = new List<Span>();
            }

            spans.Add(span);
        }
    }
}
