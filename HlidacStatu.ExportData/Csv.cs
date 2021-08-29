namespace HlidacStatu.ExportData
{
    public class Csv : FlatFile
    {
        public Csv() : base(",", FlatFile.DefaultConfig.Quote)
        { }
    }
}
