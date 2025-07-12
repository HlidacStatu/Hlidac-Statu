using System.Dynamic;
using System.Text.Json;
using Devmasters;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatu.Extensions;

public static class DotaceExtension
{
    
    public static async Task<bool?> MaSkutecnehoMajiteleAsync(this Dotace dotace)
    {
        if (dotace.ApprovedYear is null)
            return null;
        if (string.IsNullOrWhiteSpace(dotace.Recipient.Ico))
            return null;

        var datum = new DateTime(dotace.ApprovedYear.Value, 1, 1);
        var firma = FirmaRepo.FromIco(dotace.Recipient.Ico);
        
        if (SkutecniMajiteleRepo.PodlehaSkm(firma, datum))
        {
            var result = await SkutecniMajiteleRepo.GetAsync(firma.ICO);

            //skm nenalezen
            if (result == null)
                return false;
        }
        
        return true;
    }

    public static ExpandoObject FlatExport(this Dotace subsidy)
    {
        dynamic v = new ExpandoObject();
        v.Id = subsidy.Id;
        v.DataSource = subsidy.PrimaryDataSource;
        v.Url = subsidy.GetUrl(false);
        v.AssumedAmount = subsidy.AssumedAmount;
        v.RecipientIco = subsidy.Recipient.Ico;
        v.RecipientName = subsidy.Recipient.Name;
        v.RecipientHlidacName = subsidy.Recipient.HlidacName;
        v.RecipientYearOfBirth = subsidy.Recipient.YearOfBirth;
        v.RecipientObec = subsidy.Recipient.Obec;
        v.RecipientOkres = subsidy.Recipient.Okres;
        v.RecipientPSC = subsidy.Recipient.PSC;
        v.SubsidyAmount = subsidy.SubsidyAmount;
        v.PayedAmount = subsidy.PayedAmount;
        v.ReturnedAmount = subsidy.ReturnedAmount;
        v.ProjectCode = subsidy.ProjectCode;
        v.ProjectName = subsidy.ProjectName;
        v.ProjectDescription = subsidy.ProjectDescription;
        v.ProgramCode = subsidy.ProgramCode;
        v.ProgramName = subsidy.ProgramName;
        v.ApprovedYear = subsidy.ApprovedYear;
        v.SubsidyProvider = subsidy.SubsidyProvider;
        v.SubsidyProviderIco = subsidy.SubsidyProviderIco;
        v.HintIsOriginal = subsidy.Hints.IsOriginal;
        v.HintsOriginalSubsidyId = subsidy.Hints.OriginalSubsidyId;
        v.HintsHasDuplicates = subsidy.Hints.HasDuplicates;
        v.HintsCategory1 = subsidy.Hints.Category1;
        v.HintsCategory2 = subsidy.Hints.Category2;
        v.HintsCategory3 = subsidy.Hints.Category3;
        v.HintsRecipientStatus = subsidy.Hints.RecipientStatus;
        v.HintsSubsidyType = subsidy.Hints.SubsidyType;
        v.HintsRecipientStatusFull = subsidy.Hints.RecipientStatusFull;
        v.HintsRecipientTypSubjektu = subsidy.Hints.RecipientTypSubjektu;
        v.HintsRecipientPolitickyAngazovanySubjekt = subsidy.Hints.RecipientPolitickyAngazovanySubjekt;
        v.HintsRecipientPocetLetOdZalozeni = subsidy.Hints.RecipientPocetLetOdZalozeni;


        return v;
    }

    public static string? GetNiceRawData(this Subsidy subsidy)
    {
        if(subsidy.RawData is null)
            return null;
        
        return JsonSerializer.Serialize(subsidy.RawData, new JsonSerializerOptions
        {
            WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }
    public static string? DescribeDataSource(string primaryDataSource)
    {
        if (Dotace.DataSourceDescription.TryGetValue(primaryDataSource, out var description))
        {
            return description;
        }

        return string.Empty;
    }

    public static string? DescribeDataSource(this Dotace subsidy)
    {
        return DescribeDataSource(subsidy.PrimaryDataSource);
    }
    
}