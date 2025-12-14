using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.DS.Api
{
    public partial class OcrWork
    {
        public class Result
        {
            public enum ResultStatus
            {
                Valid = 1,
                Invalid = 0,
                InQueueWithCallback = 3,
                Unknown = -1
            }
            public string Id { get; set; }

            public class Document
            {
                public class KeyVal
                {
                    public string Key { get; set; }
                    public string Value { get; set; }
                }

                public string ContentType { get; set; }

                private string _filename = default(string);
                public string Filename
                {
                    get { return _filename; }
                    set
                    {
                        _filename = value ?? "neznamy.bin";
                        this.ContentType = MimeMapping.MimeUtility.GetMimeMapping(_filename);
                    }
                }

                public string Text { get; set; }
                public float Confidence { get; set; }
                public bool UsedOCR { get; set; } = false;
                public int Pages { get; set; }

                public decimal RemainsInSec { get; set; }

                public string UsedTool { get; set; }
                public string Server { get; set; }

                public KeyVal[] FileMetadata = new KeyVal[] { };

            }

            public List<Document> Documents { get; set; } = new List<Document>();

            Document _mergedDocument = null;
            public Document MergedDocuments()
            {
                if (_mergedDocument == null)
                {
                   
                    _mergedDocument = MergeDocumentsIntoOne(this.Documents);
                }
                return _mergedDocument;
            }

            public static Document MergeDocumentsIntoOne(IEnumerable<Document> docs)
            {

                Document doc = new Document();

                if (docs.Count() == 0)
                {
                    doc.Text = "";
                }
                else if (docs.Count() == 1)
                {
                    doc = docs.First();
                }
                else if (docs.Count() > 1)
                {
                    doc.Text = string.Join("\n\n\n\n\n\n", docs
                        .Select(m => $"--------- soubor : {m.Filename} ---------" + m.Text));
                    doc.Pages = docs.Sum(m => m.Pages);
                    doc.RemainsInSec = docs.Sum(m => m.RemainsInSec);
                    doc.UsedOCR = docs.Any(m => m.UsedOCR);
                    doc.UsedTool = string.Join("|", docs.Select(m => m.UsedTool));
                    doc.Confidence = docs.Average(m => m.Confidence);
                    doc.ContentType = docs.First().ContentType;
                    doc.FileMetadata = docs.SelectMany(m=>m.FileMetadata).ToArray();
                    doc.Filename = docs.First().Filename;
                    doc.Server = docs.First().Server;
                    
                }
                return doc;
            }

            public string Server { get; set; } = System.Environment.MachineName;
            public DateTime Started { get; set; } = DateTime.Now;
            public DateTime Ends { get; set; }

            public ResultStatus IsValid { get; set; } = ResultStatus.Unknown;
            public string Error { get; set; } = null;


            public TimeSpan Remains()
            {
                return Ends - Started;
            }

            public void FinishOK()
            {
                this.Ends = DateTime.Now;
                this.IsValid = Result.ResultStatus.Valid;
            }

            public void SetException(Exception e)
            {
                this.Error = e.Message;
                this.Ends = DateTime.Now;
                if (e.InnerException != null)
                    this.Error = this.Error + "\n" + e.InnerException.Message;
                this.IsValid = Result.ResultStatus.Invalid;
            }

            public string ToJson()
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(this);
            }
        }
    }
}