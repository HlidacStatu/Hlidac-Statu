using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public class CamelotResult
    {
        public class Table
        {
            public string Content { get; set; }
            public long Page { get; set; }
            public long TableInPage { get; set; }
        }
        public enum Statuses
        {
            Started,
            Running,
            Done,
            Error
        }
        public enum Formats
        {
            CSV,
            JSON,
            HTML,
            EXCEL,
            SQLITE
        }

        public string SessionId { get; set; }
        public string Status { get; set; }
        public string ScriptOutput { get; set; }
        public Table[] Tables { get; set; } = new Table[] { };
        public int FoundTables { get; set; }
        public string Format { get; set; }
        public string Algorithm { get; set; }
        public long ElapsedTimeInMs { get; set; }

        public bool ErrorOccured()
        {
            return this.Status.ToLower() == "error";
        }

    }

}
