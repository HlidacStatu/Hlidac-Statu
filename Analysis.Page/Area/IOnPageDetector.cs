namespace HlidacStatu.Analysis.Page.Area
{
    public interface IOnPageDetector : IDisposable
    {
        void AnalyzeImage();
        void RenderBoundariesToImage(string modifiedImageFilename);
        FoundBoxes Result();
    }
}
