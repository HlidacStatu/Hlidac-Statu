using System.IO;
using HlidacStatu.Entities;

namespace HlidacStatu.Connectors.IO
{
    public class PrilohaFile : DistributedFilePath<Smlouva>
    {
        public enum RequestedFileType
        {
            Original,
            PDF
        }

        public PrilohaFile()
            : this(Devmasters.Config.GetWebConfigValue("PrilohyDataPath"))
        { }

        public PrilohaFile(string root)
        : base(3, root, (s) => { return Devmasters.Crypto.Hash.ComputeHashToHex(s.Id).Substring(0, 3) + Path.DirectorySeparatorChar + s.Id; })
        {
        }
        public override string GetFullDir(Smlouva obj)
        {
            return base.GetFullDir(obj) + obj.Id + Path.DirectorySeparatorChar;
        }
        public string GetFullPath(Smlouva obj, Smlouva.Priloha priloha, RequestedFileType filetype = RequestedFileType.Original)
        {
            return GetFullPath(obj, priloha.odkaz, filetype);
        }
        public override string GetFullPath(Smlouva obj, string prilohaUrl)
        {
            throw new System.NotImplementedException("Use GetFullPath(Smlouva obj, string prilohaUrl, RequestedFileType filetype = RequestedFileType.Original)");
        }
        public string GetFullPath(Smlouva obj, string prilohaUrl, RequestedFileType filetype = RequestedFileType.Original)
        {
            if (string.IsNullOrEmpty(prilohaUrl) || obj == null)
                return string.Empty;
            var fn = GetFullDir(obj) + Encode(prilohaUrl);
            if (filetype == RequestedFileType.PDF)
                fn = fn + ".pdf";
            return fn;
        }


        public override string GetRelativeDir(Smlouva obj)
        {
            return base.GetRelativeDir(obj) + obj.Id + Path.DirectorySeparatorChar;
        }
        
        
        public override string GetRelativePath(Smlouva obj, string prilohaUrl)
        {
            throw new System.NotImplementedException("Use GetRelativePath(Smlouva obj, string prilohaUrl, RequestedFileType filetype = RequestedFileType.Original)");

        }

        public static string Encode(string prilohaUrl)
        {
            return Devmasters.Crypto.Hash.ComputeHashToHex(prilohaUrl);
            //using (MD5 md5Hash = MD5.Create())
            //{
            //    byte[] md5= md5Hash.ComputeHash(Encoding.UTF8.GetBytes(prilohaUrl));
            //    return System.Convert.ToBase64String(md5.ToString("X")).Replace("=","-");
            //}
        }



    }
}
