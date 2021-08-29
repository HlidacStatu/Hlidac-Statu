namespace HlidacStatu.Connectors.IO
{
    public class UploadedTmpFile : DistributedFilePath<string>
    {
        public UploadedTmpFile()
            : this(Devmasters.Config.GetWebConfigValue("PrilohyDataPath") + "\\_TmpUploaded")
        { }

        public UploadedTmpFile(string root)
        : base(1, root, (s) => s)
        {
            InitPaths();
        }

    }
}
