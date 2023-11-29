namespace HlidacStatu.DS.Api
{
    public partial class TablesInDoc
    {


        public class ApiOldCamelotResult : TablesInDoc.Result
        {

            public string CamelotServer { get; set; }

            public string SessionId { get; set; }
            public string ScriptOutput { get; set; }

            public bool ErrorOccured()
            {
                return this.Status.ToLower() == "error";
            }

        }

    }
}