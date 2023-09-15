using Nest;
using System;

namespace HlidacStatu.Lib.Analysis.KorupcniRiziko
{
    [ElasticsearchType(IdProperty = nameof(Id))]
    public class KindexFeedback
    {
        [Keyword]
        public string Id { get; set; }
        [Keyword]
        public int Year { get; set; }
        [Date]
        public DateTime? SignDate { get; set; }
        [Keyword]
        public string Ico { get; set; }
        [Text]
        public string Company { get; set; }
        [Text]
        public string Text { get; set; }
        [Text]
        public string Author { get; set; }
        
    }
}
