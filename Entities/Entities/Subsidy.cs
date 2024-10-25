using System;
using System.Security.Cryptography;
using System.Text;
using Nest;

namespace HlidacStatu.Entities.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public class Subsidy
{
    [Keyword]
    public string Id
    {
        get
        {
            var recipientBytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{Common.Recipient.Ico}_{Common.Recipient.Name}_{Common.Recipient.YearOfBirth}_{Common.Recipient.Obec}")); 
            var databytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{DataSource}_{Common.ProjectCode}_{Common.ProjectName}_{Common.ProgramName}_{Common.ProgramCode}_{Common.ApprovedDateString}_{Common.SubsidyProvider}_{Common.SubsidyProviderIco}"));
            var dataHash = Convert.ToHexString(databytes);
            var recipientHash = Convert.ToHexString(recipientBytes);
            return $"{dataHash}_{recipientHash}";
        }
    }
    
    /// <summary>
    /// Filename which was processed
    /// </summary>
    [Keyword]
    public string FileName { get; set; }
    
    /// <summary>
    /// Who manages the data about this subsidy 
    /// </summary>
    [Text]
    public string DataSource { get; set; }
    
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
    /// If true, then there was an update and this subsidy was missing in update file.
    /// Basically it tells us if DataSource removed the subsidy.
    /// </summary>
    [Boolean]
    public bool IsHidden { get; set; } = false;

    public class CommonInfo
    {
        [Object]
        public Recipient Recipient { get; set; } = new();

        [Number]
        public decimal? SubsidyAmount { get; set; }

        [Number]
        public decimal? PayedAmount { get; set; }
        
        [Number]
        public decimal? ReturnedAmount { get; set; }

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
        
        /// <summary>
        /// Do not use this field - this is just for Id generation
        /// </summary>
        [Text]
        public string ApprovedDateString { get; set; }
        
        [Text]
        public string SubsidyProvider { get; set; }
        [Keyword]
        public string SubsidyProviderIco { get; set; }
        
    }
    
    public class Recipient
    {
        [Keyword]
        public string Ico { get; set; }
        [Text]
        public string Name { get; set; }
        [Text]
        public string HlidacName { get; set; }
        [Keyword]
        public string HlidacNameId { get; set; }
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