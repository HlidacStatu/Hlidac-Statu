using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Cache;
using Nest;

namespace HlidacStatu.Extensions
{
    public static class KindexExtension
    {
        private static async Task<(string kindComment, KIndexData.KIndexParts? usedPart)> Best(KIndexData.Annual data, int year, string ico)
        {
            KIndexData.KIndexParts? usedPart = (await KindexCache.GetKindexOrderedValuesFromBestForInfofactsAsync(data, ico))?.FirstOrDefault();
            if (usedPart != null)
            {
                return (KIndexData.KIndexCommentForPart(usedPart.Value, data), usedPart);
            }

            return (null, null);
        }

        private static async Task<(string kindComment, KIndexData.KIndexParts? usedPart)> Worst(KIndexData.Annual data, int year, string ico)
        {
            KIndexData.KIndexParts? usedPart = (await KindexCache.GetKindexOrderedValuesFromBestForInfofactsAsync(data, ico))?.Reverse()?.FirstOrDefault();
            if (usedPart != null)
            {
                return (KIndexData.KIndexCommentForPart(usedPart.Value, data), usedPart);
            }

            return (null, null);
        }
        

        public static async Task<InfoFact[]> InfoFactsAsync(this KIndexData kIndexData, int year)
        {
            var ann = kIndexData.roky.Where(m => m.Rok == year).FirstOrDefault();

            List<InfoFact> facts = new List<InfoFact>();
            if (ann == null || ann.KIndexReady == false)
            {
                facts.Add(new InfoFact(
                    $"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.",
                    Fact.ImportanceLevel.Summary));
                return facts.ToArray();
            }

            switch (ann.KIndexLabel)
            {
                case KIndexData.KIndexLabelValues.None:
                    facts.Add(new InfoFact(
                        $"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.",
                        Fact.ImportanceLevel.Summary));
                    return facts.ToArray();

                case KIndexData.KIndexLabelValues.A:
                    facts.Add(
                        new InfoFact("Nevykazuje téměř žádné rizikové faktory.", Fact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.B:
                    facts.Add(new InfoFact("Chování s malou mírou rizikových faktorů.",
                        Fact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.C:
                    facts.Add(new InfoFact("Částečně umožňuje rizikové jednání.", Fact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.D:
                    facts.Add(new InfoFact("Vyšší míra rizikových faktorů.", Fact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.E:
                    facts.Add(new InfoFact("Vysoký výskyt rizikových faktorů.", Fact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.F:
                    facts.Add(new InfoFact("Velmi vysoká míra rizikových faktorů.", Fact.ImportanceLevel.Summary));
                    break;
                default:
                    break;
            }
            
            (var sBest, var bestPart) = await Best(ann, year, kIndexData.Ico);
            (var sworst, var worstPart) = await Worst(ann, year, kIndexData.Ico);

            //A-C dej pozitivni prvni
            if (ann.KIndexLabel == KIndexData.KIndexLabelValues.A
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.B
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.C
               )
            {
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, Fact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, Fact.ImportanceLevel.Stat));
            }
            else
            {
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, Fact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, Fact.ImportanceLevel.Stat));
            }

            var kindexOrderedValuesFromBest = await KindexCache.GetKindexOrderedValuesFromBestForInfofactsAsync(ann, kIndexData.Ico);
            foreach (var part in kindexOrderedValuesFromBest.Reverse())
            {
                if (part != bestPart && part != worstPart)
                {
                    var sFacts = KIndexData.KIndexCommentForPart(part, ann);
                    if (!string.IsNullOrEmpty(sFacts))
                        facts.Add(new InfoFact(sFacts, Fact.ImportanceLevel.High));
                }
            }

            return facts.ToArray();
        }

        //KindexData
        public static async Task<InfoFact[]> InfoFactsAsync(this KIndexData kIndexData)
        {
            var kidx = kIndexData.LastReadyKIndex();
            return await kIndexData.InfoFactsAsync(kidx?.Rok ?? (await KIndexRepo.GetAvailableCalculationYearsAsync()).Max());
        }
    }
}