using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;
using HlidacStatu.Repositories.Statistics;

namespace HlidacStatu.Extensions;

public static class OsobaExtension2
{
    private static DateTime minLookBack = new DateTime(DateTime.Now.Year - 5, 1, 1);

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
                     || osoba.StatistikaRegistrSmluv().SmlouvyStat_SoukromeFirmySummary()
                         .Summary().PocetSmluv > 0;

        if (showIt)
            return showIt;

        var dotace = await DotaceRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 0, 0,
            ((int)HlidacStatu.Repositories.Searching.DotaceSearchResult.DotaceOrderResult.FastestForScroll).ToString(),
            exactNumOfResults: true);
        showIt = dotace.Total > 0;
        if (showIt)
            return showIt;

        var dataSets = (await DataSetCache.GetProductionDatasetsAsync())
            .Where(m => m.DatasetId != "skutecni-majitele").ToArray();
        var dsQuery = $"( OsobaId:ilja-pospisil) ";
        showIt = await DatasetRepo.Searching.CheckIfAnyRecordExistAsync(dsQuery, dataSets);

        return showIt;
    }

    public static async Task<bool> MaVztahySeStatemAsync(this Osoba osoba)
    {
        if (osoba.IsSponzor())
            return true;

        var cts = new CancellationTokenSource();

        List<Task<HlidacStatu.Searching.Search.ISearchResult>> tasks = new()
        {
            Util.Helper.CreateBaseTask<HlidacStatu.Searching.Search.ISearchResult, SmlouvaSearchResult>(
                SmlouvaRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 1, 1, 0,
                    cancellationToken: cts.Token)),
            Util.Helper.CreateBaseTask<HlidacStatu.Searching.Search.ISearchResult, VerejnaZakazkaSearchData>(
                VerejnaZakazkaRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, null, 1, 1, "0",
                    cancellationToken: cts.Token)),
            Util.Helper.CreateBaseTask<HlidacStatu.Searching.Search.ISearchResult, DotaceSearchResult>(
                DotaceRepo.Searching.SimpleSearchAsync("osobaid:" + osoba.NameId, 1, 1, "0",
                    cancellationToken: cts.Token)),
        };


        while (tasks.Any())
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            HlidacStatu.Searching.Search.ISearchResult result = await completedTask;
            if (result.Total > 0)
            {
                cts.Cancel();
                return true;
            }
        }

        return false;
    }

    public static bool IsPolitikBasedOnEvents(this Osoba osoba)
    {
        var ret = osoba.Events(ev =>
                ev.Type == (int)OsobaEvent.Types.Politicka
                || ev.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                || ev.Type == (int)OsobaEvent.Types.VolenaFunkce
            )
            .Where(ev =>
                (ev.DatumDo.HasValue && ev.DatumDo > minLookBack) ||
                (ev.DatumDo.HasValue == false && ev.DatumOd > minLookBack.AddYears(-2)))
            .ToArray();

        return ret.Count() > 0;
    }


    public static List<string> MainRoles(this Osoba osoba, int forYear)
    {
        var events = osoba.Events(ev =>
                (ev.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                 || ev.Type == (int)OsobaEvent.Types.VolenaFunkce)
                && ev.AddInfo != null
                && (ev.DatumDo == null || ev.DatumDo.Value.Year >= forYear)
                && (ev.DatumOd == null || ev.DatumOd.Value.Year <= forYear))
            .ToList();
        
        return AssembleRoles(events); 
    }
    
    public static string MainRolesToString(this Osoba osoba, int forYear, string delimiter = ", ")
    {
        var events = osoba.Events(ev =>
                (ev.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                 || ev.Type == (int)OsobaEvent.Types.VolenaFunkce)
                && ev.AddInfo != null
                && (ev.DatumDo == null || ev.DatumDo.Value.Year >= forYear)
                && (ev.DatumOd == null || ev.DatumOd.Value.Year <= forYear))
            .ToList();

        try
        {
            var roles = AssembleRoles(events);
                
            return string.Join(delimiter, roles);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        return "";
    }

    private static List<string> AssembleRoles(List<OsobaEvent> events)
    {
        if (events is null || !events.Any())
            return [];
        
        List<string> roles = new();

        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                && (e.AddInfo.ToLower().StartsWith("předseda vlády")
                    || e.AddInfo.ToLower().StartsWith("předsedkyně vlády"))
                && (e.Organizace?.ToLower().StartsWith("úřad vlády") == true
                    || e.Ico?.Trim() == UradyConstants.Ica.UradVlady)))
        {
            roles.Add("předseda vlády");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                && e.AddInfo.ToLower().StartsWith("ministr")
                && (e.Organizace?.ToLower().StartsWith("ministerstvo") == true
                    || UradyConstants.Ica.Vlada.Contains(e.Ico?.Trim()))))
        {
            roles.Add("ministr");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.VolenaFunkce
                && (e.AddInfo.ToLower().StartsWith("poslanec") ||
                    e.AddInfo.ToLower().StartsWith("poslankyně"))
                && (e.Organizace?.ToLower().StartsWith("poslanecká sněmovna pčr") == true
                    || e.Ico?.Trim() == UradyConstants.Ica.KancelarPoslaneckeSnemovny)))
        {
            roles.Add("poslanec");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.VolenaFunkce
                && e.AddInfo.ToLower().StartsWith("senát")
                && (e.Organizace?.ToLower().StartsWith("senát") == true
                    || e.Ico?.Trim() == UradyConstants.Ica.Senat)))
        {
            roles.Add("senátor");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.VolenaFunkce
                && (e.AddInfo.ToLower().StartsWith("poslanec ep") ||
                    e.AddInfo.ToLower().StartsWith("poslankyně ep"))
                && e.Organizace?.ToLower().StartsWith("evropský parlament") == true))
        {
            roles.Add("europoslanec");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.PolitickaExekutivni
                && e.AddInfo.ToLower().StartsWith("hejtman")))
        {
            roles.Add("hejtman");
        }
        
        if (events.Any(e => 
                e.Type == (int)OsobaEvent.Types.VolenaFunkce
                && e.AddInfo.ToLower().StartsWith("zastupitel")
                && UradyConstants.Ica.Kraje.Contains(e.Ico?.Trim())))
        {
            roles.Add("krajský zastupitel");
        }

        return roles;
    }
    

    /// <summary>
    /// returns true if changed
    /// </summary>
    public static async Task<bool> RecalculateStatusAsync(this Osoba osoba)
    {
        switch (osoba.StatusOsoby())
        {
            case Osoba.StatusOsobyEnum.NeniPolitik:
                if (osoba.IsPolitikBasedOnEvents())
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                    return true;
                }

                //TODO zkontroluj, ze neni politik podle eventu
                break;

            case Osoba.StatusOsobyEnum.VazbyNaPolitiky:
            case Osoba.StatusOsobyEnum.ByvalyPolitik:
            case Osoba.StatusOsobyEnum.Sponzor:
                if (osoba.IsPolitikBasedOnEvents())
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                    return true;
                }

                if (osoba.IsSponzor() == false && await osoba.MaVztahySeStatemAsync() == false)
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                    return true;
                }

                break;
            case Osoba.StatusOsobyEnum.Politik:
                bool chgnd = false;
                if (osoba.IsPolitikBasedOnEvents() == false)
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                    chgnd = true;
                }

                if (chgnd && osoba.IsSponzor() == false && await osoba.MaVztahySeStatemAsync() == false)
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.NeniPolitik;
                    chgnd = true;
                }
                else
                {
                    osoba.Status = (int)Osoba.StatusOsobyEnum.Politik;
                    chgnd = false;
                }

                return chgnd;
            default:
                break;
        }

        return false;
    }
}