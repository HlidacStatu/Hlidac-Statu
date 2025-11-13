using System.Linq;
using HlidacStatu.Lib.Analytics;

using System.Collections.Generic;

namespace HlidacStatu.Repositories.ES
{
    public partial class QueryGrouped
    {
        public class ResultPerIco
        {
            public List<StatisticsSubjectPerYear<SimpleStat>> TopPodlePoctu { get; set; } = new();
            public List<StatisticsSubjectPerYear<SimpleStat>> TopPodleKc { get; set; } = new();

            public List<StatisticsSubjectPerYear<SimpleStat>> CombinedTop(int count = 10)
            {
                List<StatisticsSubjectPerYear<SimpleStat>> res = new List<StatisticsSubjectPerYear<SimpleStat>>();
                int idx = 0;
                do
                {
                    if (idx < TopPodleKc.Count )
                    {
                        if (!res.Any(m => m.ICO == this.TopPodleKc[idx].ICO))
                            res.Add(this.TopPodleKc[idx]);
                    }
                    if (idx < TopPodlePoctu.Count && res.Count < count)
                    {
                        if (!res.Any(m => m.ICO == this.TopPodlePoctu[idx].ICO))
                            res.Add(this.TopPodlePoctu[idx]);
                    }

                    idx++;

                } while (res.Count < count && (idx < TopPodlePoctu.Count || idx < TopPodleKc.Count));

                return res;
            }

        }
        public class ResultPerYear
        {

            public Dictionary<int, List<(string ico, SimpleStat stat)>> TopPodlePoctu { get; set; }
            public Dictionary<int, List<(string ico, SimpleStat stat)>> TopPodleKc { get; set; }

            public IEnumerable<(string ico, SimpleStat stat)> CombinedTop(int year, int count = 10)
            {
                if (TopPodleKc is not null && TopPodleKc.Count > 0)
                {
                    IEnumerable<(string ico, SimpleStat stat)> res1 = TopPodleKc[year]
                        .Where(o=>o.stat.CelkemCena>0)
                        .OrderByDescending(o => o.stat.CelkemCena)
                        .Take(count);
                    var icos = res1.Select(m => m.ico).Distinct();
                    
                    IEnumerable<(string ico, SimpleStat stat)> res2 = TopPodleKc[year]
                        .Where(o => o.stat.Pocet > 0 && icos.Contains(o.ico)==false)
                        .OrderByDescending(o => o.stat.Pocet)
                        .Take(count - res1.Count() + 4);

                    return res1
                        .Concat(res2)
                        .Take(count);
                }
                
                return Enumerable.Empty<(string ico, SimpleStat stat)>();
            }

        }

        public class ResultCombined
        {
            public ResultPerIco PerIco { get; set; } = new ResultPerIco();
            public ResultPerYear PerYear { get; set; } = new ResultPerYear();
        }

    }
}
