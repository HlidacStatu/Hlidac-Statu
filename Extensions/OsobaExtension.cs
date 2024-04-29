using HlidacStatu.Datasets;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Statistics;

namespace HlidacStatu.Extensions;

public static class OsobaExtension2
{
    public static async Task<bool> IsInterestingToShowAsync(this Osoba osoba)
    {
        if (osoba.NameId == "radek-jonke")
            return false;
        
        var showIt = osoba.StatusOsoby() == Osoba.StatusOsobyEnum.Politik
             || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.ByvalyPolitik
             || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.Sponzor
             || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.VysokyUrednik
             || osoba.StatusOsoby() == Osoba.StatusOsobyEnum.VazbyNaPolitiky
             || osoba.IsSponzor()
             || await osoba.MaVztahySeStatemAsync()
             || osoba.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny).SoukromeFirmySummary()
                 .Summary().PocetSmluv > 0;

        if (showIt)
            return showIt;
        
        var dotace = await DotaceRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 0, 0,
            ((int)HlidacStatu.Repositories.Searching.DotaceSearchResult.DotaceOrderResult.FastestForScroll).ToString(),
            exactNumOfResults: true);
        showIt = dotace.Total > 0;
        if (showIt)
            return showIt;
        
        var dataSets = DataSetDB.ProductionDataSets.Get()
            .Where(m => m.DatasetId != "skutecni-majitele").ToArray();
        var dsQuery = $"( OsobaId:ilja-pospisil) ";
        showIt = await DatasetRepo.Searching.CheckIfAnyRecordExistAsync(dsQuery, dataSets);
        
        return showIt;
    }
    
}