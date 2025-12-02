using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities.KIndex;
using Nest;

namespace HlidacStatu.Repositories.Analysis.KorupcniRiziko
{
    public partial class Statistics
    {
        public class PercInterval
        {
            public PercInterval()
            {
            }

            public PercInterval(int value)
            {
                From = value;
                To = value;
            }

            public PercInterval(int from, int to)
            {
                From = from;
                To = to;
            }

            public int From { get; set; }
            public int To { get; set; }
        }


        public static async Task<IEnumerable<Statistics>> CalculateAsync(string[] forIcos = null, bool futureKIDX = false)
        {
            var client = Manager.GetESClient_KIndex();
            if (futureKIDX)
                client = Manager.GetESClient_KIndexTemp();

            int[] calculationYears = Consts.ToCalculationYears;
            Func<int, int, Task<ISearchResponse<KIndexData>>> searchfnc = null;
            if (forIcos != null)
            {
                searchfnc = (size, page) => client.SearchAsync<KIndexData>(a => a
                    .Size(size)
                    .From(page * size)
                    .Query(q => q.Terms(t => t.Field(f => f.Ico).Terms(forIcos)))
                    .Scroll("10m")
                );
            }
            else
            {
                searchfnc = (size, page) => client.SearchAsync<KIndexData>(a => a
                    .Size(size)
                    .From(page * size)
                    .Query(q => q.MatchAll())
                    .Scroll("10m")
                );
            }

            List<KIndexData> data = new List<KIndexData>();
            await Repositories.Searching.Tools.DoActionForQueryAsync<KIndexData>(client,
                searchfnc,
                (hit, _) =>
                {
                    if (hit.Source.roky.Any(m => m.KIndexReady))
                        data.Add(hit.Source);


                    return Task.FromResult(new Devmasters.Batch.ActionOutputData() { CancelRunning = false, Log = null });
                }, null, Devmasters.Batch.Manager.DefaultOutputWriter, Devmasters.Batch.Manager.DefaultProgressWriter,
                false, prefix: "GET data ");


            List<Statistics> stats = new List<Statistics>();

            foreach (var year in calculationYears)
            {
                var datayear = data
                    .Where(m => (forIcos == null || forIcos?.Contains(m.Ico) == true))
                    .Select(m => new { ic = m.Ico, data = m.ForYear(year) })
                    .Where(y => y != null && y.data != null && y.data.KIndexReady)
                    .ToDictionary(k => k.ic, v => v.data);

                if (datayear.Count == 0)
                    continue;

                var stat = new Statistics() { Rok = year };
                //poradi
                stat.SubjektOrderedListKIndexAsc = datayear
                    .Where(m => m.Value.KIndexReady)
                    .Select(m =>
                    {
                        var company = Firmy.Get(m.Key);
                        return new IcoDetail() { ico = m.Key, kindex = m.Value.KIndex, krajId = company.KrajId };
                    })
                    .OrderBy(m => m.kindex)
                    .ToList();

                foreach (KIndexData.KIndexParts part in Enum.GetValues(typeof(KIndexData.KIndexParts)))
                {
                    stat.SubjektOrderedListPartsAsc.Add(part, datayear
                        .Where(m => m.Value.KIndexReady)
                        .Where(m => m.Value.KIndexVypocet.Radky.Any(r => r.VelicinaPart == part))
                        .Select(m =>
                        {
                            var company = Firmy.Get(m.Key);
                            return new IcoDetail()
                            {
                                ico = m.Key,
                                kindex = m.Value.KIndexVypocet.Radky.First(r => r.VelicinaPart == part).Hodnota,
                                krajId = company.KrajId
                            };
                            
                        })
                        .OrderBy(m => m.kindex)
                        .ToList()
                    );
                }

                //prumery
                stat.AverageKindex = datayear.Average(m => m.Value.KIndex);

                stat.AverageParts = new KIndexData.VypocetDetail();
                List<KIndexData.VypocetDetail.Radek> radky = new List<KIndexData.VypocetDetail.Radek>();
                foreach (KIndexData.KIndexParts part in Enum.GetValues(typeof(KIndexData.KIndexParts)))
                {
                    decimal val = 0;
                    var tmp = datayear
                        .Select(m => m.Value.KIndexVypocet.Radky
                            .FirstOrDefault(r => r.Velicina == (int)part))
                        .Where(a => a != null);
                    if (tmp.Count() > 0)
                        val = tmp.Average(a => a.Hodnota);

                    KIndexData.VypocetDetail.Radek radek = new KIndexData.VypocetDetail.Radek(part, val,
                        KIndexData.DetailInfo.DefaultKIndexPartKoeficient(part));
                    radky.Add(radek);
                }

                stat.AverageParts = new KIndexData.VypocetDetail() { Radky = radky.ToArray() };

                //percentily
                stat.PercentileKIndex = new Dictionary<int, decimal>();
                stat.PercentileParts = new Dictionary<int, KIndexData.VypocetDetail>();

                foreach (var perc in Percentiles)
                {
                    stat.PercentileKIndex.Add(perc,
                        Util.MathTools.PercentileCont(perc / 100m, datayear.Select(m => m.Value.KIndex))
                    );

                    radky = new List<KIndexData.VypocetDetail.Radek>();
                    foreach (KIndexData.KIndexParts part in Enum.GetValues(typeof(KIndexData.KIndexParts)))
                    {
                        decimal val = Util.MathTools.PercentileCont(perc / 100m,
                            datayear
                                .Select(m => m.Value.KIndexVypocet.Radky.FirstOrDefault(r => r.Velicina == (int)part))
                                .Where(m => m != null)
                                .Select(m => m.Hodnota)
                        );
                        KIndexData.VypocetDetail.Radek radek = new KIndexData.VypocetDetail.Radek(part, val,
                            KIndexData.DetailInfo.DefaultKIndexPartKoeficient(part));
                        radky.Add(radek);
                    }

                    stat.PercentileParts.Add(perc, new KIndexData.VypocetDetail() { Radky = radky.ToArray() });
                }

                stats.Add(stat);
            } //year

            return stats;
        }


        public static string PercIntervalShortText(PercInterval val)
        {
            if (val.To <= 50)
            {
                if (val.To <= 10)
                    return $"je mezi {val.To} % nejlepších";
                else if (val.To <= 33)
                    return $"patří mezi první třetinu nejlepších";
                else
                    return "je v lepší polovině";
            }
            else
            {
                if (val.From >= 90)
                    return $"je mezi {100 - val.From} % nejhorších";
                else if (val.From >= 66)
                    return $"patří do třetiny nejhorších";
                else
                    return "průměrné, v horší polovině";
            }
        }
    }
}