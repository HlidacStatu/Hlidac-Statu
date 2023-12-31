// ---------------------------------------------------------------------------
// <copyright file="PositionAlignment.cs" owner="svm-git">
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
    /// An alignment for the cue box in the dimension of the writing direction, describing what the position is anchored to.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-position-line-left-alignment for details.</remarks>
    public enum PositionAlignment
    {
        /// <summary>
        /// The cue box’s left side (for horizontal cues) or top side (otherwise) is aligned at the position.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-position-line-left-alignment for details.</remarks>
        LineLeft,

        /// <summary>
        /// The cue box is centered at the position. 
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-line-center-alignment for details.</remarks>
        Center,

        /// <summary>
        /// The cue box’s bottom side (for horizontal cues), right side (for vertical growing right), or left side (for vertical growing left) is aligned at the line.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-cue-line-end-alignment for details.</remarks>
        LineRight
    }
}
