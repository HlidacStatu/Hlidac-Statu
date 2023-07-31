using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.DS.Api
{
    public partial class TablesInDoc
    {


        public class ApiResult : TablesInDoc.Result
        {
            public enum Formats
            {
                CSV,
                JSON,
                HTML,
                EXCEL,
                SQLITE
            }

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