using MimeDetective;
using MimeDetective.Storage;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace HlidacStatu.Util
{
    public static class FileMime
    {

        static FileMime()
        {
            var allDef = new MimeDetective.Definitions.CondensedBuilder()
            {
                UsageType = MimeDetective.Definitions.Licensing.UsageType.PersonalNonCommercial
            }.Build();

            var pdfExt = new[] { "pdf" }.ToImmutableHashSet(StringComparer.InvariantCultureIgnoreCase);

            var pdfScopedDef = allDef
                .ScopeExtensions(pdfExt) //Limit results to only the extensions provided
                .TrimMeta() //If you don't care about the meta information (definition author, creation date, etc)
                .TrimDescription() //If you don't care about the description
                                   //.TrimMimeType() //If you don't care about the mime type
                .ToImmutableArray()
                ;

            pdfInspector = new ContentInspectorBuilder()
            {
                Definitions = pdfScopedDef,
            }.Build();
        }
        static ContentInspector pdfInspector = null;

        public static bool HasPdfHeader(string filename)
        {
            System.Collections.Immutable.ImmutableArray<MimeDetective.Engine.FileExtensionMatch> types = pdfInspector.Inspect(filename).ByFileExtension();
            return (types.Any(m => m.Extension == "pdf")) ;
        }

        public static bool HasPDFHeaderFast(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;
            if (!System.IO.File.Exists(filename))
                return false;

            byte[] b = new byte[4];
            using (var r = System.IO.File.OpenRead(filename))
            {
                r.Read(b, 0, 4);
            }
            byte[] pdfheader = new byte[] { 37, 80, 68, 70 };
            bool valid = true;
            for (int i = 0; i < 4; i++)
            {
                valid = valid && b[i] == pdfheader[i];
            }
            return valid;
        }


    }
}
