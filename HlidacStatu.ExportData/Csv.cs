namespace HlidacStatu.ExportData
{
    public class Csv : FlatFile
    {
        public Csv() : base(",", FlatFile.DefaultConfig.Quote,true)
        { }

        public Csv(string delimiter, char quote, bool quoteAll) : base(delimiter, quote, quoteAll)
        {
        }
    }
}
