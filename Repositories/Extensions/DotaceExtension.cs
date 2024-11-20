using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Dotace;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatu.Extensions;

public static class DotaceExtension
{
    public static async Task<bool> MaSkutecnehoMajiteleAsync(this Dotace dotace)
    {
        var datum = dotace.DatumPodpisu;
        if (datum is null)
        {
            var minYear = dotace.Rozhodnuti.MinBy(k => k.Rok).Rok;
            if (minYear is null || minYear == 0)
                return true;

            datum = new DateTime(minYear.Value, 1, 1);
        }
        
        var firma = FirmaRepo.FromIco(dotace.Prijemce.Ico);
        if (SkutecniMajiteleRepo.PodlehaSkm(firma, datum.Value))
        {
            var result = await SkutecniMajiteleRepo.GetAsync(firma.ICO);

            //skm nenalezen
            if (result == null)
                return false;
        }
        
        return true;
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