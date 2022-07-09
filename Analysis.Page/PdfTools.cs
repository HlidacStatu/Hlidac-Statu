using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Analysis.Page
{
    public static class PdfTools
    {
        public static int GetPageCount(System.IO.Stream str)
        {
            return PDFtoImage.Conversion.GetPageCount(str);
        }

        public static void SaveJpeg(string jpegFileName, Stream pdfStr, int page)
        {
            PDFtoImage.Conversion.SaveJpeg(jpegFileName, pdfStr, page: page);
        }
    }
}
