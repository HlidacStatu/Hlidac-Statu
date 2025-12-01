using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories;
using Nest;

namespace HlidacStatu.Extensions
{
    public static class KindexExtension
    {
        private static string Best(KIndexData.Annual data, int year, string ico, out KIndexData.KIndexParts? usedPart)
        {
            usedPart = data.OrderedValuesFromBestForInfofacts(ico).FirstOrDefault();
            if (usedPart != null)
            {
                return KIndexData.KIndexCommentForPart(usedPart.Value, data);
            }

            return null;
        }

        private static string Worst(KIndexData.Annual data, int year, string ico, out KIndexData.KIndexParts? usedPart)
        {
            usedPart = data.OrderedValuesFromBestForInfofacts(ico)?.Reverse()?.FirstOrDefault();
            if (usedPart != null)
            {
                return KIndexData.KIndexCommentForPart(usedPart.Value, data);
            }

            return null;
        }


        static Devmasters.Cache.LocalMemory.Manager<KIndexData.KIndexParts[], (KIndexData.Annual, string)>
            _orderedValuesFromBestForInfofactsCache
                = Devmasters.Cache.LocalMemory.Manager<KIndexData.KIndexParts[], (KIndexData.Annual, string)>
                    .GetSafeInstance("orderedValuesFromBestForInfofacts",
                        (data) => await OrderedValuesFromBestForInfofactsAsync(data.Item1, data.Item2),
                        TimeSpan.FromDays(2), (data) => $"{data.Item1.Ico}_{data.Item1.Rok}-{data.Item2}"
                    );

        public static KIndexData.KIndexParts[] OrderedValuesFromBestForInfofacts(this KIndexData.Annual annual,
            string ico, bool invalidateCache = false)
        {
            if (invalidateCache)
                _orderedValuesFromBestForInfofactsCache.Delete((annual, ico));

            return _orderedValuesFromBestForInfofactsCache.Get((annual, ico));
        }

        private static async Task<KIndexData.KIndexParts[]> OrderedValuesFromBestForInfofactsAsync(
            this KIndexData.Annual annual, string ico)
        {
            if (annual._orderedValuesForInfofacts == null)
            {
                await annual.AnnualSemaphoreSlim.WaitAsync();
                try
                {
                    if (annual._orderedValuesForInfofacts == null)
                    {
                        var stat = await HlidacStatu.Repositories.Analysis.KorupcniRiziko.Statistics
                            .GetStatisticsAsync(annual.Rok); //todo: může být null, co s tím?
                        if (annual.KIndexVypocet.Radky != null || annual.KIndexVypocet.Radky.Count() > 0)

                            annual._orderedValuesForInfofacts = annual.KIndexVypocet.Radky
                                .Select(m => new { r = m, rank = stat.SubjektRank(ico, m.VelicinaPart) })
                                .Where(m => m.rank.HasValue)
                                .Where(m =>
                                        m.r.VelicinaPart !=
                                        KIndexData.KIndexParts.PercNovaFirmaDodavatel //nezajimava oblast
                                        && !(m.r.VelicinaPart == KIndexData.KIndexParts.PercSmlouvyPod50kBonus &&
                                             m.r.Hodnota == 0) //bez bonusu
                                )
                                .OrderBy(m => m.rank)
                                .ThenBy(o => o.r.Hodnota)
                                .Select(m => m.r.VelicinaPart)
                                .ToArray(); //better debug
                        else
                            annual._orderedValuesForInfofacts = new KIndexData.KIndexParts[] { };
                    }
                }
                finally
                {
                    annual.AnnualSemaphoreSlim.Release();
                }
            }

            return annual._orderedValuesForInfofacts;
        }

        public static InfoFact[] InfoFacts(this KIndexData kIndexData, int year)
        {
            var ann = kIndexData.roky.Where(m => m.Rok == year).FirstOrDefault();

            List<InfoFact> facts = new List<InfoFact>();
            if (ann == null || ann.KIndexReady == false)
            {
                facts.Add(new InfoFact(
                    $"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.",
                    InfoFact.ImportanceLevel.Summary));
                return facts.ToArray();
            }

            switch (ann.KIndexLabel)
            {
                case KIndexData.KIndexLabelValues.None:
                    facts.Add(new InfoFact(
                        $"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.",
                        InfoFact.ImportanceLevel.Summary));
                    return facts.ToArray();

                case KIndexData.KIndexLabelValues.A:
                    facts.Add(
                        new InfoFact("Nevykazuje téměř žádné rizikové faktory.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.B:
                    facts.Add(new InfoFact("Chování s malou mírou rizikových faktorů.",
                        InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.C:
                    facts.Add(new InfoFact("Částečně umožňuje rizikové jednání.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.D:
                    facts.Add(new InfoFact("Vyšší míra rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.E:
                    facts.Add(new InfoFact("Vysoký výskyt rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.F:
                    facts.Add(new InfoFact("Velmi vysoká míra rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                default:
                    break;
            }

            KIndexData.KIndexParts? bestPart = null;
            KIndexData.KIndexParts? worstPart = null;
            var sBest = Best(ann, year, kIndexData.Ico, out bestPart);
            var sworst = Worst(ann, year, kIndexData.Ico, out worstPart);

            //A-C dej pozitivni prvni
            if (ann.KIndexLabel == KIndexData.KIndexLabelValues.A
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.B
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.C
               )
            {
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, InfoFact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, InfoFact.ImportanceLevel.Stat));
            }
            else
            {
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, InfoFact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, InfoFact.ImportanceLevel.Stat));
            }

            foreach (var part in ann.OrderedValuesFromBestForInfofacts(kIndexData.Ico).Reverse())
            {
                if (part != bestPart && part != worstPart)
                {
                    var sFacts = KIndexData.KIndexCommentForPart(part, ann);
                    if (!string.IsNullOrEmpty(sFacts))
                        facts.Add(new InfoFact(sFacts, InfoFact.ImportanceLevel.High));
                }
            }

            return facts.ToArray();
        }

        //KindexData
        public static async Task<InfoFact[]> InfoFactsAsync(this KIndexData kIndexData)
        {
            var kidx = kIndexData.LastReadyKIndex();
            return kIndexData.InfoFacts(kidx?.Rok ?? (await KIndexRepo.GetAvailableCalculationYearsAsync()).Max());
        }
    }
}