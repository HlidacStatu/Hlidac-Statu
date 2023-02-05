using HlidacStatu.Util;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HlidacStatu.XLib.Watchdogs
{
    public class RenderedContent
    {
        public string ContentText { get; set; }
        public string ContentHtml { get; set; }
        public IEnumerable<SMTPTools.EmbeddedImage> Images { get; set; } = null;
        public string ContentTitle { get; set; }

        public static RenderedContent Merge(IEnumerable<RenderedContent> tojoin)
        {
            StringBuilder sbT = new StringBuilder(1024);
            StringBuilder sbH = new StringBuilder(1024);
            List<SMTPTools.EmbeddedImage> images = new List<SMTPTools.EmbeddedImage>();
            foreach (var i in tojoin)
            {
                _ = sbH.AppendLine(i.ContentHtml);
                _ = sbT.AppendLine(i.ContentText);
                if (i.Images?.Count() > 0)
                    images.AddRange(i.Images);
            }
            return new RenderedContent() { ContentHtml = sbH.ToString(), ContentText = sbT.ToString(), Images = images };
        }
    }
}
