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


        const float a4height = 854;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="jpegFileName"></param>
        /// <param name="pdfStr"></param>
        /// <param name="page">zero base page number</param>
        /// <exception cref="ApplicationException"></exception>
        public static void SaveJpeg(string jpegFileName, Stream pdfStr, int page)
        {
            float maxSize = 0.0F;
            try
            {
                iText.Kernel.Pdf.PdfDocument pdf = new iText.Kernel.Pdf.PdfDocument(new iText.Kernel.Pdf.PdfReader(pdfStr));
                var p = pdf.GetPage(page+1);
                var pagesize = p.GetPageSize();
                maxSize = Math.Max(pagesize.GetWidth(), pagesize.GetHeight());
                pdf.Close();
            }
            catch (Exception e)
            {
                throw new ApplicationException("Cannot open PDF ", e);
            }

            int dpi = 300;
            if (maxSize > 1200)
            {
                dpi =(int)( dpi * (a4height / maxSize));
                if (dpi < 72)
                    dpi = 72;
            }

            PDFtoImage.Conversion.SaveJpeg(jpegFileName, pdfStr, page: page, dpi:dpi);
        }
    }
}
