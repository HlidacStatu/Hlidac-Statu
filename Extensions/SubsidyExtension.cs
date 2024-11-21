using HlidacStatu.Entities.Entities;
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
}