using System.Collections.Generic;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Statistics
{
    public static class OsobaStatistics
    {
       
        public static async Task<Osoba.Statistics.RegistrSmluv> CachedStatistics_SmlouvyAsync(Osoba os,
            int? obor, bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateOsobaSmlouvyStatisticsAsync(os, obor);

            return await StatisticsCache.GetOsobaSmlouvyStatisticsAsync(os, obor);
        }

        public static async Task<Osoba.Statistics.Dotace> CachedStatistics_DotaceAsync(Osoba os,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateOsobaDotaceStatisticsAsync(os);

            return await StatisticsCache.GetOsobaDotaceStatisticsAsync(os);
        }
        

        public static async Task<string[]> SmlouvyStat_NeziskovkyAsync(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkyIcos == null)
            {
                var neziskovkyIcos = new List<string>();
                foreach (var firma in registrSmluv.SoukromeFirmy)
                {
                    var f = await Firmy.GetAsync(firma.Key);
                    if (f.JsemNeziskovka())
                    {
                        neziskovkyIcos.Add(f.ICO);
                    }
                }
                registrSmluv._neziskovkyIcos = neziskovkyIcos.ToArray();
            }

            return registrSmluv._neziskovkyIcos;
        }

        public static async Task<int> SmlouvyStat_NeziskovkyCountAsync(this Osoba.Statistics.RegistrSmluv registrSmluv) =>
            (await registrSmluv.SmlouvyStat_NeziskovkyAsync()).Count();

        public static async Task<int> SmlouvyStat_KomercniFirmyCountAsync(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            return registrSmluv.SoukromeFirmy.Count - await registrSmluv.SmlouvyStat_NeziskovkyCountAsync();
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_SoukromeFirmySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._soukromeFirmySummary == null)
                registrSmluv._soukromeFirmySummary = registrSmluv.SoukromeFirmy?.Values.AggregateStats();

            return registrSmluv._soukromeFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_StatniFirmySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._statniFirmySummary == null)
                registrSmluv._statniFirmySummary = registrSmluv.StatniFirmy?.Values.AggregateStats();

            return registrSmluv._statniFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_NeziskovkySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkySummary == null)
                registrSmluv._neziskovkySummary = registrSmluv.Neziskovky?.Values.AggregateStats();

            return registrSmluv._neziskovkySummary;
        }
    }
}