using System.Dynamic;
using System.Text.Json;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatu.Extensions;

public static class SubsidyExtension
{
    public static string GetUrl(this Subsidy subsidy, bool local = true, bool enableRedirectToOriginal = true) => GetUrl(subsidy, local, string.Empty, enableRedirectToOriginal);

    public static string GetUrl(this Subsidy subsidy, bool local, string foundWithQuery, bool enableRedirectToOriginal = true)
    {
        string url = "/Dotace/Detail/" + System.Net.WebUtility.UrlEncode(subsidy.Id) + "?";
        if (!string.IsNullOrEmpty(foundWithQuery))
            url = url + "qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);
        if (enableRedirectToOriginal == false)
            url = url + "r=false";

        if (url.EndsWith("?"))
            url = url.Substring(0, url.Length - 1);

        if (local == false)
            return "https://www.hlidacstatu.cz" + url;
        else
            return url;
    }
    
    public static async Task<bool?> MaSkutecnehoMajiteleAsync(this Subsidy dotace)
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

    public static ExpandoObject FlatExport(this Subsidy subsidy)
    {
        dynamic v = new ExpandoObject();
        v.Url = subsidy.GetUrl(false);
        v.Id = subsidy.Id;
        v.FileName = subsidy.Metadata.FileName;
        v.DataSource = subsidy.Metadata.DataSource;
        v.IsHidden = subsidy.Metadata.IsHidden;
        v.AssumedAmount = subsidy.AssumedAmount;
        v.CommonInfoRecipientIco = subsidy.Recipient.Ico;
        v.CommonInfoRecipientName = subsidy.Recipient.Name;
        v.CommonInfoRecipientHlidacName = subsidy.Recipient.HlidacName;
        v.CommonInfoRecipientYearOfBirth = subsidy.Recipient.YearOfBirth;
        v.CommonInfoRecipientObec = subsidy.Recipient.Obec;
        v.CommonInfoRecipientOkres = subsidy.Recipient.Okres;
        v.CommonInfoRecipientPSC = subsidy.Recipient.PSC;
        v.CommonInfoSubsidyAmount = subsidy.SubsidyAmount;
        v.CommonInfoPayedAmount = subsidy.PayedAmount;
        v.CommonInfoReturnedAmount = subsidy.ReturnedAmount;
        v.CommonInfoProjectCode = subsidy.ProjectCode;
        v.CommonInfoProjectName = subsidy.ProjectName;
        v.CommonInfoProjectDescription = subsidy.ProjectDescription;
        v.CommonInfoProgramCode = subsidy.ProgramCode;
        v.CommonInfoProgramName = subsidy.ProgramName;
        v.CommonInfoApprovedYear = subsidy.ApprovedYear;
        v.CommonInfoSubsidyProvider = subsidy.SubsidyProvider;
        v.CommonInfoSubsidyProviderIco = subsidy.SubsidyProviderIco;

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
      
    public static string? Describe(this Subsidy subsidy)
    {
        if (Subsidy.DataSourceDescription.TryGetValue(subsidy.Metadata.DataSource, out var description))
        {
            return description;
        }

        return null;
    }
}