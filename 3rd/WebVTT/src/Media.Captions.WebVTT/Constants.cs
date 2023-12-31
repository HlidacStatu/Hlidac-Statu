// ---------------------------------------------------------------------------
// <copyright file="Constants.cs" owner="svm-git">
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
    /// <summary>
    /// Utility class for various string literals defined in WebVTT format.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#syntax for details.</remarks>
    internal static class Constants
    {
        public const string WebVttHeaderToken = "WEBVTT";
        public const string RegionToken = "REGION";
        public const string StyleToken = "STYLE";
        public const string CommentToken = "NOTE";
        public const string ArrowToken = "-->";

        public const string RegionIdName = "id";
        public const string WidthName = "width";
        public const string LinesName = "lines";
        public const string RegionAnchorName = "regionanchor";
        public const string ViewPortAnchorName = "viewportanchor";
        public const string ScrollName = "scroll";
        
        public const string VerticalName = "vertical";
        public const string LineName = "line";
        public const string PositionName = "position";
        public const string SizeName = "size";
        public const string AlignName = "align";
        public const string RegionName = "region";

        public const string ScrollUpValue = "up";
        public const string StartValue = "start";
        public const string CenterValue = "center";
        public const string EndValue = "end";
        public const string LeftValue = "left";
        public const string RightValue = "right";
        public const string LineLeftValue = "line-left";
        public const string LineRightValue = "line-right";
        public const string RightToLeftValue = "rl";
        public const string LeftToRightValue = "lr";

        public const string ClassSpanName = "c";
        public const string ItalicsSpanName = "i";
        public const string BoldSpanName = "b";
        public const string UnderlineSpanName = "u";
        public const string RubySpanName = "ruby";
        public const string RubyTextSpanName = "rt";
        public const string VoiceSpanName = "v";
        public const string LanguageSpanName = "lang";
    }
}
