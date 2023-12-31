// ---------------------------------------------------------------------------
// <copyright file="SpanType.cs" owner="svm-git">
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
    /// Determines the type of a caption or subtitle cue components.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-caption-or-subtitle-cue-components for details.</remarks>
    public enum SpanType
    {
        /// <summary>
        /// A WebVTT cue class span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-class-span for details.</remarks>
        Class,

        /// <summary>
        /// A WebVTT cue italics span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-italics-span for details.</remarks>
        Italics,

        /// <summary>
        /// A WebVTT cue bold span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-bold-span for details.</remarks>
        Bold,

        /// <summary>
        /// A WebVTT cue underline span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-underline-span for details.</remarks>
        Underline,

        /// <summary>
        /// A WebVTT cue ruby span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-ruby-span for details.</remarks>
        Ruby,

        /// <summary>
        /// A WebVTT cue ruby span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-ruby-span for details.</remarks>
        RubyText,

        /// <summary>
        /// A WebVTT cue voice span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-voice-span for details.</remarks>
        Voice,

        /// <summary>
        /// A WebVTT cue language span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-language-span for details.</remarks>
        Language,

        /// <summary>
        /// A WebVTT cue timestamp.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-timestamp for details.</remarks>
        TimeStamp,

        /// <summary>
        /// A WebVTT cue text span.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-text-span for details.</remarks>
        Text
    }
}
