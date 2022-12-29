using System.IO;
using HlidacStatu.Entities;

namespace HlidacStatu.Connectors.IO
{
    public class OsobaFotkyFile : DistributedFilePath<Osoba>
    {
        public OsobaFotkyFile()
            : this(Devmasters.Config.GetWebConfigValue("OsobaFotkyDataPath"))
        { }

        public OsobaFotkyFile(string root)
        : base(2, root, (s) => { return Devmasters.Crypto.Hash.ComputeHashToHex(s.NameId).Substring(0, 2) + Path.DirectorySeparatorChar + s.NameId; })
        {
            InitPaths();
        }

        protected override string GetHash(Osoba obj)
        {
            return funcToGetId(obj);
        }


        public override string GetFullDir(Osoba obj)
        {
            var path = base.GetFullDir(obj);
            return path.Substring(0, path.Length - 1) + "-";
        }

        public override string GetRelativeDir(Osoba obj)
        {
            var path = base.GetRelativeDir(obj);
            return path.Substring(0, path.Length - 1) + "-";
        }

    }
}
