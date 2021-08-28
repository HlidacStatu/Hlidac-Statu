namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public class CamelotResult  : Result
    {
        public enum Formats
        {
            CSV,
            JSON,
            HTML,
            EXCEL,
            SQLITE
        }

        public string SessionId { get; set; }
        public string ScriptOutput { get; set; }

        public bool ErrorOccured()
        {
            return this.Status.ToLower() == "error";
        }

    }

}
