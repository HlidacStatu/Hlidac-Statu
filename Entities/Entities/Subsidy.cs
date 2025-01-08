using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Devmasters;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public partial class Subsidy
{

    [Keyword]
    public string Id
    {
        get
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{Recipient.Ico}_{Recipient.Name}_{Recipient.YearOfBirth}_{Recipient.Obec}" +
                $"_{ProjectCode}_{ProjectName}_{ProgramName}_{ProgramCode}" +
                $"_{ApprovedYear}_{SubsidyProvider}_{SubsidyProviderIco}"));
            var hash = Convert.ToHexString(hashBytes);
            return $"{Metadata.DataSource}-{hash}";
        }
    }
    
    /// <summary>
    /// Hash that helps to check duplicity
    /// </summary>
    [Keyword]
    public string DuplaHash {
        get
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{Recipient.Ico}_{ApprovedYear}_{AssumedAmount.ToString("N0", CultureInfo.InvariantCulture)}"));
            return Convert.ToHexString(hashBytes);
            
        }
    }
    [Keyword]
    public string ProjectCodeHash => CalculateDuplaHash(ProjectCode);
    [Keyword]
    public string ProjectNameHash => CalculateDuplaHash(ProjectName);
    
    [Keyword]
    public string OriginalId { get; set; }
    

    [Object]
    public SubsidyMetadata Metadata { get; set; } = new();
    
    [Number]
    public decimal AssumedAmount =>
        (PayedAmount is null || PayedAmount == 0)
            ? SubsidyAmount ?? 0m
            : PayedAmount.Value;
    
    
    /// <summary>
    /// Oblast kam dotace patří
    /// </summary>
    public string Category { get; set; }

    [Object]
    public SubsidyRecipient Recipient { get; set; } = new();

    [Number]
    public decimal? SubsidyAmount { get; set; }

    [Number]
    public decimal? PayedAmount { get; set; }

    [Number]
    public decimal? ReturnedAmount { get; set; }
        
    public string ProjectCode { get; set; }

    public string ProjectName { get; set; }

    [Text]
    public string ProjectDescription { get; set; }
        
    [Keyword]
    public string ProgramCode { get; set; }

    public string ProgramName { get; set; }

    [Number]
    public int? ApprovedYear { get; set; }

    public string SubsidyProvider { get; set; }

    [Keyword]
    public string SubsidyProviderIco { get; set; }

    [Ignore]
    public string DisplayProject => string.IsNullOrWhiteSpace(ProjectName) ? ProjectCode : ProjectName;

    [Object]
    public List<RozhodnutiItem> Rozhodnuti { get; set; } = new();
    [Object]
    public List<CerpaniItem> Cerpani { get; set; } = new();
    
    /// <summary>
    /// Original record for people to display in case of need
    /// </summary>
    [Object(Enabled = false)] //do not index, do not process, just store
    public List<object> RawData { get; set; } = new();

    [Object]
    public Hint Hints { get; set; } = new();
    
    public class SubsidyMetadata
    {
        [Keyword]
        public string FileName { get; set; }

        /// <summary>
        /// Who manages the data about this subsidy (Folder name) 
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// Order within a filename
        /// </summary>
        [Keyword]
        public int RecordNumber { get; set; }

        [Date]
        public DateTime ProcessedDate { get; set; }
    
        [Date]
        public DateTime ModifiedDate { get; set; }
        
        /// <summary>
        /// If true, then there was an update and this subsidy was missing in update file.
        /// Basically it tells us if DataSource removed the subsidy.
        /// </summary>
        [Boolean]
        public bool IsHidden { get; set; } = false;


    }

    public class SubsidyRecipient
    {
        [Keyword]
        public string Ico { get; set; }

        public string Name { get; set; }

        public string HlidacName { get; set; }

        [Keyword]
        public string HlidacNameId { get; set; }

        [Number]
        public int? YearOfBirth { get; set; }

        public string Obec { get; set; }

        public string Okres { get; set; }

        [Keyword]
        public string PSC { get; set; }

        [Ignore]
        public string DisplayName => string.IsNullOrWhiteSpace(HlidacName) ? Name : HlidacName;
    }
    
    public class RozhodnutiItem
    {
        public string? FinancniZdroj { get; set; }
        [Number]
        public decimal? Amount { get; set; }
    }
    
    public class CerpaniItem
    {
        [Number]
        public int? Year { get; set; }
        [Number]
        public decimal? Amount { get; set; }
    }
    
    private static string CalculateDuplaHash(string text)
    {
        if (text is null || string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim().RemoveDiacritics();
        text = Regex.Replace(text, @"\s{1,}", "_");
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes);

    }
}