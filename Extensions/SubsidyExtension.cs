using System.Dynamic;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatu.Extensions;

public static class SubsidyExtension
{
    public static string GetUrl(this Subsidy subsidy, bool local = true) => GetUrl(subsidy, local, string.Empty);

    public static string GetUrl(this Subsidy subsidy, bool local, string foundWithQuery)
    {
        string url = "/Dotace/Detail/" + subsidy.Id;
        if (!string.IsNullOrEmpty(foundWithQuery))
            url = url + "?qs=" + System.Net.WebUtility.UrlEncode(foundWithQuery);

        if (local == false)
            return "https://www.hlidacstatu.cz" + url;
        else
            return url;
    }
    
    public static async Task<bool?> MaSkutecnehoMajiteleAsync(this Subsidy dotace)
    {
        if (dotace.Common.ApprovedYear is null)
            return null;
        if (string.IsNullOrWhiteSpace(dotace.Common.Recipient.Ico))
            return null;

        var datum = new DateTime(dotace.Common.ApprovedYear.Value, 1, 1);
        var firma = FirmaRepo.FromIco(dotace.Common.Recipient.Ico);
        
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
        v.FileName = subsidy.FileName;
        v.DataSource = subsidy.DataSource;
        v.IsHidden = subsidy.IsHidden;
        v.AssumedAmount = subsidy.AssumedAmount;
        v.CommonInfoRecipientIco = subsidy.Common.Recipient.Ico;
        v.CommonInfoRecipientName = subsidy.Common.Recipient.Name;
        v.CommonInfoRecipientHlidacName = subsidy.Common.Recipient.HlidacName;
        v.CommonInfoRecipientYearOfBirth = subsidy.Common.Recipient.YearOfBirth;
        v.CommonInfoRecipientObec = subsidy.Common.Recipient.Obec;
        v.CommonInfoRecipientOkres = subsidy.Common.Recipient.Okres;
        v.CommonInfoRecipientPSC = subsidy.Common.Recipient.PSC;
        v.CommonInfoSubsidyAmount = subsidy.Common.SubsidyAmount;
        v.CommonInfoPayedAmount = subsidy.Common.PayedAmount;
        v.CommonInfoReturnedAmount = subsidy.Common.ReturnedAmount;
        v.CommonInfoProjectCode = subsidy.Common.ProjectCode;
        v.CommonInfoProjectName = subsidy.Common.ProjectName;
        v.CommonInfoProjectDescription = subsidy.Common.ProjectDescription;
        v.CommonInfoProgramCode = subsidy.Common.ProgramCode;
        v.CommonInfoProgramName = subsidy.Common.ProgramName;
        v.CommonInfoApprovedYear = subsidy.Common.ApprovedYear;
        v.CommonInfoSubsidyProvider = subsidy.Common.SubsidyProvider;
        v.CommonInfoSubsidyProviderIco = subsidy.Common.SubsidyProviderIco;

        return v;
    }
}