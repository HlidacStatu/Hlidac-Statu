using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public class Result
    {
        public class Table
        {
            public string Content { get; set; }
            public long Page { get; set; }
            public long TableInPage { get; set; }

            public string[][] ParsedContent()
            {
                string[][] cells = new string[0][];

                var json = Newtonsoft.Json.Linq.JArray.Parse(this.Content);
                var numRows = json.Count;
                var numCells = 0;
                if (numRows > 0)
                {
                    cells = new string[numRows][];
                    for (int r = 0; r < numRows; r++)
                    {
                        var row = (Newtonsoft.Json.Linq.JObject)json[r];
                        cells[r] = new string[row.Count];
                        for (int c = 0; c < row.Count; c++)
                        {
                            cells[r][c] = row.GetValue(c.ToString()).Value<string>();
                        }

                    }
                }

                return cells;
            }
        }
        public enum Statuses
        {
            Started,
            Running,
            Done,
            Error
        }
        /// <summary>
        /// values from Statuses
        /// </summary>
        public string Status { get; set; }
        public Table[] Tables { get; set; } = new Table[] { };
        public int FoundTables { get; set; }
        public string Format { get; set; }
        public string Algorithm { get; set; }   
        public long ElapsedTimeInMs { get; set; }

    }

}
