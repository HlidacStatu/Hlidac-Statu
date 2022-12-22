using HlidacStatu.Entities;
using static HlidacStatu.Entities.Smlouva;

namespace HlidacStatu.Connectors.IO
{
    public class VZPrilohaFile : DistributedFilePath<string>
    {
        public VZPrilohaFile()
            : this(Devmasters.Config.GetWebConfigValue("VZPrilohyDataPath"))
        { }

        public VZPrilohaFile(string root)
        : base(3, root, (prilohaId) => { return Devmasters.Crypto.Hash.ComputeHashToHex(prilohaId).Substring(0, 3) ; })
        {
        }
        public override string GetFullDir(string prilohaId)
        {
            return base.GetFullDir(prilohaId) + prilohaId + "\\";
        }
        public string GetFullPath(string prilohaId)
        {
            return GetFullPath(prilohaId, prilohaId);
        }

        public override string GetFullPath(string prilohaId, string filename)
        {
            if (string.IsNullOrEmpty(prilohaId) )
                return string.Empty;
            return GetFullDir(prilohaId) + prilohaId;
        }


        public override string GetRelativeDir(string prilohaId)
        {
            return base.GetRelativeDir(prilohaId) + "\\";
        }
        public string GetRelativePath(string prilohaId)
        {
            return GetRelativePath(prilohaId, prilohaId);
        }
        public override string GetRelativePath(string prilohaId, string prilohaFilename)
        {
            if (string.IsNullOrEmpty(prilohaId) )
                return string.Empty;
            return GetRelativeDir(prilohaId) + prilohaId;

            //return base.GetRelativePath(obj, Devmasters.Crypto.Hash.ComputeHash(prilohaUrl));
        }


        //public static string Encode(string prilohaUrl)
        //{
        //    return Devmasters.Crypto.Hash.ComputeHashToHex(prilohaUrl);
        //    //using (MD5 md5Hash = MD5.Create())
        //    //{
        //    //    byte[] md5= md5Hash.ComputeHash(Encoding.UTF8.GetBytes(prilohaUrl));
        //    //    return System.Convert.ToBase64String(md5.ToString("X")).Replace("=","-");
        //    //}
        //}



    }
}
