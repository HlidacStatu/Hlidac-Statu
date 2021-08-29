namespace HlidacStatu.ExportData
{
    public class TabDelimited : FlatFile
    {
        public TabDelimited() : base("\t", FlatFile.DefaultConfig.Quote)
        { }
    }
}
