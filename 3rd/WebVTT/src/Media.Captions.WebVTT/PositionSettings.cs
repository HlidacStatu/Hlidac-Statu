// ---------------------------------------------------------------------------
// <copyright file="PositionSettings.cs" owner="svm-git">
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
    /// Configures the indent position of the cue box in the direction orthogonal to the line cue setting.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-position-cue-setting for details.</remarks>
    public struct PositionSettings
    {
        /// <summary>
        /// The cue position as a percentage of the video viewport. 
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-position-cue-setting for details.</remarks>
        public double? PositionPercent;

        /// <summary>
        /// An alignment for the cue box in the dimension of the writing direction, describing what the position is anchored to.
        /// </summary>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-position-cue-setting for details.</remarks>
        public PositionAlignment? Alignment;
    }
}
