using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Analysis.Page.Area
{
    public interface IOnPageDetector : IDisposable
    {
        void AnalyzeImage();
        void RenderBoundariesToImage(string modifiedImageFilename);
        FoundBoxes Result();
    }
}
