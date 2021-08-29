using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.OCR.Api
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
            //private static MimeMapping mime = new MimeMapping.Mime();
            public string ContentType { get; set; }

            private string _filename = default(string);
            public string Filename
            {
                get { return _filename; }
                set
                {
                    _filename = value ?? "neznamy.bin";
                    ContentType = MimeMapping.MimeUtility.GetMimeMapping(_filename);
                }
            }

            public string Text { get; set; }
            public float Confidence { get; set; }
            public bool UsedOCR { get; set; } = false;
            public int Pages { get; set; }

            public decimal RemainsInSec { get; set; }

            public string UsedTool { get; set; }
            public string Server { get; set; }

        }

        public List<Document> Documents { get; set; } = new List<Document>();

        Document _mergedDocument = null;
        public Document MergedDocuments()
        {
            if (_mergedDocument == null)
            {
                Document doc = new Document();

                if (Documents.Count == 0)
                {
                    doc.Text = "";
                }
                else if (Documents.Count == 1)
                {
                    doc = Documents[0];
                }
                else if (Documents.Count > 1)
                {
                    doc.Text = Documents
                        .Select(m => $"--------- soubor : {m.Filename} ---------" + m.Text)
                        .Aggregate((f, s) => f + "\n\n\n\n\n\n" + s);
                    doc.Pages = Documents.Sum(m => m.Pages);
                    doc.RemainsInSec = Documents.Sum(m => m.RemainsInSec);
                    doc.UsedOCR = Documents.Any(m => m.UsedOCR);
                    doc.UsedTool = Documents.Select(m => m.UsedTool).Aggregate((f, s) => f + "|" + s);
                    doc.Confidence = Documents.Average(m => m.Confidence);
                }
                _mergedDocument = doc;
            }
            return _mergedDocument;
        }

        public string Server { get; set; } = Environment.MachineName;
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
            Ends = DateTime.Now;
            IsValid = ResultStatus.Valid;
        }

        public void SetException(Exception e)
        {
            Error = e.Message;
            Ends = DateTime.Now;
            if (e.InnerException != null)
                Error = Error + "\n" + e.InnerException.Message;
            IsValid = ResultStatus.Invalid;
        }

        public string ToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }
    }
}
