// ---------------------------------------------------------------------------
// <copyright file="TextAlignment.cs" owner="svm-git">
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
    /// Alignment of the text within the cue.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-alignment-cue-setting for details.</remarks>
    public enum TextAlignment
    {
        /// <summary>
        /// Align to the start of the line, relative to base text direction.
        /// </summary>
        Start,

        /// <summary>
        /// Align to the center of the line.
        /// </summary>
        Center,

        /// <summary>
        /// Align to the end of the line, relative to base text direction.
        /// </summary>
        End,

        /// <summary>
        /// Align to the left of the line.
        /// </summary>
        Left,

        /// <summary>
        /// Align to the right of the line.
        /// </summary>
        Right
    }
}
