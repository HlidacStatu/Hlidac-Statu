using Newtonsoft.Json.Linq;

namespace HlidacStatu.DS.Api
{
    public partial class TablesInDoc
    {
        public class Result
        {
            public class Table
            {
                string _context = "";
                public string Content
                {
                    get { return _context; }
                    set { _parsedContent = null; _context = value; }
                }
                public long Page { get; set; }
                public long TableInPage { get; set; }


                private string[][] _parsedContent = null;
                public string[][] ParsedContent()
                {
                    if (_parsedContent == null)
                    {
                        string[][] cells = new string[0][];

                        var json = Newtonsoft.Json.Linq.JArray.Parse(this.Content);
                        var numRows = json.Count;

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
                        _parsedContent = cells;
                    }
                    return _parsedContent;
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
            [Nest.Keyword()]
            public string Status { get; set; }
            public Table[] Tables { get; set; } = new Table[] { };
            public int FoundTables { get; set; }
            [Nest.Keyword()]
            public string Format { get; set; }
            [Nest.Keyword()]
            public string Algorithm { get; set; }
            public long ElapsedTimeInMs { get; set; }

        }

    }
}