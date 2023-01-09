using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;

namespace HlidacStatu.Entities
{
    public partial class DocTables
    {



        string _id = null;

        [Description("Unikátní ID zaznamu. Nevyplňujte, ID se vygeneruje samo.")]
        [Nest.Keyword]
        public string Id
        {
            get
            {
                if (_id == null)
                    _id = GetId();

                return this._id;
            }
            set
            {
                _id = value;
            }
        }

        public string GetId()
        {
            return GetId(this.SmlouvaId, this.PrilohaId);
        }
        public static string GetId(string smlouvaId, string prilohaId)
        {
            if (string.IsNullOrEmpty(smlouvaId) || string.IsNullOrEmpty(prilohaId))
                return null;
            return $"{smlouvaId}_{prilohaId}";
        }


        [Nest.Date]
        public DateTime Updated { get; set; }
        [Nest.Keyword]
        public string SmlouvaId { get; set; }
        [Nest.Keyword]
        public string PrilohaId { get; set; }

        public Result[] Tables { get; set; }


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
