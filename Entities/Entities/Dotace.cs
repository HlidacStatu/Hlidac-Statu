using System;
using System.Collections.Generic;
using System.Linq;
using Devmasters;
using Nest;

namespace HlidacStatu.Entities;

[ElasticsearchType(IdProperty = nameof(Id))]
public partial class Dotace
{

    [Keyword]
    public string Id { get; set; }


    [Keyword]
    public HashSet<string> SourceIds { get; set; } = new();
    
    
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
    
    public void UpdateFromSubsidy(Subsidy subsidy)
    {
        bool isCreating = false;
        if (string.IsNullOrWhiteSpace(Id))
        {
            Id = subsidy.Id.RemoveAccents().Replace(" ", ".");
            isCreating = true;
            ProcessedDate = subsidy.Metadata.ProcessedDate;
            PrimaryDataSource = subsidy.Metadata.DataSource;
        }
        
        SourceIds.Add(subsidy.Id);
        ApprovedYear ??= subsidy.ApprovedYear;
        SubsidyAmount ??= subsidy.SubsidyAmount;
        PayedAmount ??= subsidy.PayedAmount;
        ReturnedAmount ??= subsidy.ReturnedAmount;
        Category = string.IsNullOrWhiteSpace(Category) ? subsidy.Category : Category;
        ProjectCode = string.IsNullOrWhiteSpace(ProjectCode) ? subsidy.ProjectCode : ProjectCode;
        ProjectName = string.IsNullOrWhiteSpace(ProjectName) ? subsidy.ProjectName : ProjectName;
        ProjectDescription = string.IsNullOrWhiteSpace(ProjectDescription) ? subsidy.ProjectDescription : ProjectDescription;
        ProgramCode = string.IsNullOrWhiteSpace(ProgramCode) ? subsidy.ProgramCode : ProgramCode;
        ProgramName = string.IsNullOrWhiteSpace(ProgramName) ? subsidy.ProgramName : ProgramName;
        SubsidyProvider ??= subsidy.SubsidyProvider;
        SubsidyProviderIco ??= subsidy.SubsidyProviderIco;
        
        Cerpani = !Cerpani.Any() ? subsidy.Cerpani : Cerpani;
        Rozhodnuti = !Rozhodnuti.Any() ? subsidy.Rozhodnuti : Rozhodnuti;
        
        //copy all properties for recipient
        Recipient.Ico ??= subsidy.Recipient.Ico;
        Recipient.Name ??= subsidy.Recipient.Name;
        Recipient.HlidacName ??= subsidy.Recipient.HlidacName;
        Recipient.YearOfBirth ??= subsidy.Recipient.YearOfBirth;
        Recipient.Obec ??= subsidy.Recipient.Obec;
        Recipient.Okres ??= subsidy.Recipient.Okres;
        Recipient.PSC ??= subsidy.Recipient.PSC;
        Recipient.HlidacNameId ??= subsidy.Recipient.HlidacNameId;
        
        //copy all properties for hints
        if (isCreating)
        {
            Hints.IsOriginal = subsidy.Hints.IsOriginal;
            Hints.OriginalSubsidyId = subsidy.Hints.OriginalSubsidyId;
            Hints.RecipientStatus = subsidy.Hints.RecipientStatus;
            Hints.SubsidyType = subsidy.Hints.SubsidyType;
            Hints.Category1 ??= subsidy.Hints.Category1;
            Hints.Category2 ??= subsidy.Hints.Category2;
            Hints.Category3 ??= subsidy.Hints.Category3;
            Hints.RecipientTypSubjektu = subsidy.Hints.RecipientTypSubjektu;
            Hints.RecipientPolitickyAngazovanySubjekt = subsidy.Hints.RecipientPolitickyAngazovanySubjekt;
            Hints.RecipientPocetLetOdZalozeni = subsidy.Hints.RecipientPocetLetOdZalozeni;
        }
    }
    
}