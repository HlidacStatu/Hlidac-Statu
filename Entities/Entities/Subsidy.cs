using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{Common.Recipient.Ico}_{Common.Recipient.Name}_{Common.Recipient.YearOfBirth}_{Common.Recipient.Obec}" +
                $"_{Common.ProjectCode}_{Common.ProjectName}_{Common.ProgramName}_{Common.ProgramCode}" +
                $"_{Common.ApprovedYear}_{Common.SubsidyProvider}_{Common.SubsidyProviderIco}"));
            var hash = Convert.ToHexString(hashBytes);
            return $"{DataSource}-{hash}";
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
    [Keyword]
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
    /// Oblast kam dotace patří
    /// </summary>
    [Keyword]
    public string Category { get; set; }

    /// <summary>
    /// Important information about subsidy (what is displayed in hlidac)
    /// </summary>
    [Object]
    public CommonInfo Common { get; set; } = new();
    
    [Object]
    public Hint Hints { get; set; } = new();

    /// <summary>
    /// Original record for people to display in case of need
    /// </summary>
    [Object(Enabled = false)] //do not index, do not process, just store
    public string RawData { get; set; }

    [Ignore]
    public string RawDataFormatted => JsonSerializer.Serialize(
        JsonSerializer.Deserialize<object>(RawData),
        new JsonSerializerOptions
        {
            WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

    /// <summary>
    /// If true, then there was an update and this subsidy was missing in update file.
    /// Basically it tells us if DataSource removed the subsidy.
    /// </summary>
    [Boolean]
    public bool IsHidden { get; set; } = false;

    [Number]
    public decimal AssumedAmount =>
        (Common.PayedAmount is null || Common.PayedAmount == 0)
            ? Common.SubsidyAmount ?? 0m
            : Common.PayedAmount.Value;

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

        [Keyword]
        public string ProjectCode { get; set; }

        [Text]
        public string ProjectName { get; set; }

        [Text]
        public string ProjectDescription { get; set; }
        
        [Keyword]
        public string ProgramCode { get; set; }

        [Text]
        public string ProgramName { get; set; }

        [Date]
        public int? ApprovedYear { get; set; }

        [Text]
        public string SubsidyProvider { get; set; }

        [Keyword]
        public string SubsidyProviderIco { get; set; }

        [Ignore]
        public string DisplayProject => string.IsNullOrWhiteSpace(ProjectName) ? ProjectCode : ProjectName;
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
    
    public class Hint
    {
        /// <summary>
        /// Info zda-li se jedná o pravděpodobnou duplicitní dotace
        /// </summary>
        [Keyword]
        public bool IsDuplicate { get; set; } = false;
        
        /// <summary>
        /// Pokud jde o duplicitní dotaci, tak odkaz na originál zde
        /// </summary>
        [Keyword]
        public string OriginalSubsidyId { get; set; }
        
        /// <summary>
        /// Legislativa - info, jestli jde o státní/evropskou dotaci, krajskou, obecní a nebo investiční pobídka
        /// </summary>
        [Keyword]
        public Type SubsidyType { get; set; }
        
        public enum Type
        {
            Unknown,
            Evropska,
            Krajska,
            Obecni,
            InvesticniPobidka,
        }
        
    }
}