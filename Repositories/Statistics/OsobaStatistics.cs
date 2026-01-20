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
        

        public static string[] SmlouvyStat_Neziskovky(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkyIcos == null)
            {
                registrSmluv._neziskovkyIcos = registrSmluv.SoukromeFirmy
                    .Select(m => Firmy.GetAsync(m.Key))
                    .Where(ff => ff.JsemNeziskovka())
                    .Select(s => s.ICO)
                    .ToArray();
            }

            return registrSmluv._neziskovkyIcos;
        }

        public static int SmlouvyStat_NeziskovkyCount(this Osoba.Statistics.RegistrSmluv registrSmluv) =>
            registrSmluv.SmlouvyStat_Neziskovky().Count();

        public static int SmlouvyStat_KomercniFirmyCount(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            return registrSmluv.SoukromeFirmy.Count - registrSmluv.SmlouvyStat_NeziskovkyCount();
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