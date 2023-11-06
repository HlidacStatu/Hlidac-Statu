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
                .ToImmutableArray()
                ;

            pdfInspector = new ContentInspectorBuilder()
            {
                Definitions = pdfScopedDef,
            }.Build();

            fileTypeInspector = new ContentInspectorBuilder()
            {
                Definitions = allDef
            }.Build();


        }
        static ContentInspector pdfInspector = null;
        static ContentInspector fileTypeInspector = null;

        public static MimeDetective.Storage.FileType[] GetFileTypes(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return null;

            if (System.IO.File.Exists(filename) == false) { return null; }

            try
            {
                var inspect = fileTypeInspector.Inspect(filename);
                //var res =  inspect.ByMimeType();
                return inspect
                    .OrderByDescending(o => o.Points)
                    .Select(m => m.Definition.File)
                    .ToArray();

            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("Cannost Inspect {filename}", e, filename);
                return null;
            }

        }
        public static MimeDetective.Storage.FileType GetTopFileType(string filename)
        {
            var res = GetFileTypes(filename);
            if (res == null)
                return null;
            if (res.Count() == 0)
                return null;
            return res.First();
        }
        public static bool HasPdfHeader(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;

            if (System.IO.File.Exists(filename) == false) { return false; }

            try
            {
                System.Collections.Immutable.ImmutableArray<MimeDetective.Engine.FileExtensionMatch> types = pdfInspector.Inspect(filename).ByFileExtension();
                return (types.Any(m => m.Extension == "pdf"));
            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("Cannost Inspect Pdf {filename}", e, filename);
                return false;
            }

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



        public static bool HasPDFHeaderFast(byte[] data)
        {
            if (data == null)
                return false;
            if (data.Length <5) return false;

            byte[] pdfheader = new byte[] { 37, 80, 68, 70 };
            bool valid = true;
            for (int i = 0; i < 4; i++)
            {
                valid = valid && data[i] == pdfheader[i];
            }
            return valid;
        }

    }
}
