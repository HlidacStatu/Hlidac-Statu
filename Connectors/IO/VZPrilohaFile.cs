using System.IO;

namespace HlidacStatu.Connectors.IO
{
    public class VZPrilohaFile : DistributedFilePath<string>
    {
        public VZPrilohaFile()
            : this(Devmasters.Config.GetWebConfigValue("VZPrilohyDataPath"))
        { }

        public VZPrilohaFile(string root)
        : base(3, root, (prilohaId) => { return Devmasters.Crypto.Hash.ComputeHashToHex(prilohaId).Substring(0, 3); })
        {
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

        
        public string GetRelativePath(string prilohaId)
        {
            return GetRelativePath(prilohaId, prilohaId);
        }
        public override string GetRelativePath(string prilohaId, string prilohaFilename)
        {
            if (string.IsNullOrEmpty(prilohaId) )
                return string.Empty;
            return GetRelativeDir(prilohaId) + prilohaId;

        }



    }
}
