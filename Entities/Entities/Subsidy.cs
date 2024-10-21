using System;
using System.Security.Cryptography;
using System.Text;
using Nest;

namespace HlidacStatu.Entities.Entities;

[ElasticsearchType(IdProperty = nameof(UniqueId))]
public class Subsidy
{
    [Keyword]
    public string UniqueId
    {
        get
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Source}_{FileName}_{RecordNumber}"));
            return Convert.ToHexString(bytes);
        }
    }
    
    /// <summary>
    /// Filename which was processed
    /// </summary>
    [Keyword]
    public string FileName { get; set; }
    
    /// <summary>
    /// Order within a filename
    /// </summary>
    [Keyword]
    public int RecordNumber { get; set; }
    
    /// <summary>
    /// Date when subsidy was processed.
    /// </summary>
    [Date]
    public DateTime ProcessedDate { get; set; }

    /// <summary>
    /// Important information about subsidy (what is displayed in hlidac)
    /// </summary>
    [Object]
    public CommonInfo Common { get; set; } = new();
    
    /// <summary>
    /// Original record for people to display in case of need
    /// </summary>
    [Object(Enabled = false)] //do not index, do not process, just store
    public string RawData { get; set; }
    
    /// <summary>
    /// Who manages the data about this subsidy 
    /// </summary>
    [Text]
    public string Source { get; set; }
    
    /// <summary>
    /// Who manages the data about this subsidy 
    /// </summary>
    [Keyword]
    public string SourceIco { get; set; }
    
    public class CommonInfo
    {
        [Object]
        public Recipient Recipient { get; set; } = new();
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
        public int? ApprovedYear { get; set; }
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