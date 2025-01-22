using System;
using System.Collections.Generic;
using Devmasters;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public partial class Dotace
{

    [Keyword]
    public string Id {get; set;} 
    
    public string NormalizedId => Id.RemoveAccents().Replace(" ", ".");

    
    [Keyword]
    public string[] SourceIds { get; set; }
    
    [Keyword]
    public string[] Tags { get; set; }
    
    
    public string PrimaryDataSource { get; set; } //no reason for this now?

    [Date]
    public DateTime ProcessedDate { get; set; }

    [Date]
    public DateTime ModifiedDate { get; set; }
    
    
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
    
    [Object]
    public Hint Hints { get; set; } = new();
    
    

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
    
}