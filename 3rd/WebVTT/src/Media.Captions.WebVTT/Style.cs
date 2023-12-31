// ---------------------------------------------------------------------------
// <copyright file="Style.cs" owner="svm-git">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Captions.WebVTT
{
    /// <summary>
    /// A WebVTT Style block.
    /// </summary>
    /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-style-block for more details.</remarks>
    public class Style : BaseBlock
    {
        /// <summary>
        /// Creates new style block.
        /// </summary>
        /// <param name="content">Style content.</param>
        /// <returns>Style that was created.</returns>
        /// <remarks>See http://www.w3.org/TR/webvtt1/#webvtt-style-block for more details.</remarks>
        public static Style Create(string content)
        {
            if (false == string.IsNullOrEmpty(content)
                && content.Contains(Constants.ArrowToken))
            {
                throw new ArgumentException(string.Format("Comment text cannot contain '{0}'.", Constants.ArrowToken));
            }

            return new Style() { RawContent = content };
        }
    }
}
