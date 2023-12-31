// ---------------------------------------------------------------------------
// <copyright file="Span.cs" owner="svm-git">
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
    using System.Collections.Generic;

    /// <summary>
    /// Represents a caption or subtitle cue component.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-caption-or-subtitle-cue-components for details.</remarks>
    public class Span
    {
        /// <summary>
        /// Gets or sets type of the span.
        /// </summary>
        public SpanType Type { get; set; }

        /// <summary>
        /// Gets or sets classes of the span.
        /// </summary>
        public string[] Classes { get; set; }

        /// <summary>
        /// Gets or sets children spans.
        /// </summary>
        public Span[] Children { get; set; }

        /// <summary>
        /// Gets or sets span annotation.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>
        /// Gets or sets span text.
        /// </summary>
        public string Text { get; set; }
    }
}
