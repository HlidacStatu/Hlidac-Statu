// ---------------------------------------------------------------------------
// <copyright file="Comment.cs" owner="svm-git">
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

    /// <summary>
    /// A WebVTT Comment block.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-comment-block for more details.</remarks>
    public class Comment : BaseBlock
    {
        /// <summary>
        /// Creates new comment block with given content.
        /// </summary>
        /// <param name="content">Comment content.</param>
        /// <returns>Comment that was created.</returns>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-comment-block for details.</remarks>
        public static Comment Create(string content)
        {
            if (false == string.IsNullOrEmpty(content)
                && content.Contains(Constants.ArrowToken))
            {
                throw new ArgumentException(string.Format("Comment text cannot contain '{0}'.", Constants.ArrowToken));
            }

            return new Comment() { RawContent = content };
        }
    }
}
