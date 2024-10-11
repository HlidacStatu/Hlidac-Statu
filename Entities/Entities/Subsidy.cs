using System;
using Nest;

namespace HlidacStatu.Entities.Entities;

[ElasticsearchType(IdProperty = nameof(UniqueId))]
public class Subsidy
{
    [Keyword]
    public string UniqueId { get; set; }
    [Keyword]
    public string FileName { get; set; }
    [Keyword]
    public string RowNumber { get; set; }
    [Date]
    public DateTime ProcessedDate { get; set; }
    
    [Object]
    public CommonInfo Common { get; set; }
    [Object(Enabled = false)] //do not index, do not process, just store
    public string RawData { get; set; }
    
    [Text]
    public string Source { get; set; }
    [Keyword]
    public string SourceIco { get; set; }
    
    public class CommonInfo
    {
        [Object]
        public Recipient Recipient { get; set; }
        [Number]
        public decimal? SubsidyAmount { get; set; }
        [Number]
        public decimal? PayedAmount { get; set; }
        [Text]
        public string ProjectCode { get; set; }
        [Text]
        public string ProjectName { get; set; }
        [Text]
        public string ProgramCode { get; set; }
        [Text]
        public string ProgramName { get; set; }
        [Date]
        public DateTime ApprovedDate { get; set; }
    }
    
    public class Recipient
    {
        [Keyword]
        public string Ico { get; set; }
        [Text]
        public string Name { get; set; }
        [Text]
        public string HlidacName { get; set; }
        [Number]
        public int? YearOfBirth { get; set; }

        [Keyword]
        public string Obec { get; set; }
        [Keyword]
        public string Okres { get; set; }
        [Keyword]
        public string PSC { get; set; }
        
        [Ignore]
        public string DisplayName => string.IsNullOrWhiteSpace(HlidacName) ? Name : HlidacName;
    }

}