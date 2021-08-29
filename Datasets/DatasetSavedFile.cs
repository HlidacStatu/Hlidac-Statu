using HlidacStatu.Connectors.IO;

namespace HlidacStatu.Datasets
{
    public class DatasetSavedFile : DistributedFilePath<DataSet.Item.SavedFile>
    {
        public DatasetSavedFile(DataSet ds)
            : this(ds, Devmasters.Config.GetWebConfigValue("DatasetFilePath"))
        { }

        public DatasetSavedFile(DataSet ds, string root)
        : base(2,
              root.EndsWith("\\") ? root + ds.DatasetId : root + "\\" + ds.DatasetId,
              (s) => { return Devmasters.Crypto.Hash.ComputeHashToHex(s.ItemId).Substring(0, 2); })
        {
            InitPaths();
        }

        protected override string GetHash(DataSet.Item.SavedFile obj)
        {
            return funcToGetId(obj);
        }


    }
}
