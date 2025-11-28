using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Analysis
{
    public class TemplatedQuery
    {
        public class AHref
        {
            public AHref() { }
            public AHref(string url, string text)
            {
                Url = url; Text = text;
            }
            public string Url { get; set; }
            public string Text { get; set; }
        }
        public TemplatedQuery()
        {
        }

        public string Query { get; set; }
        public AHref[] Links { get; set; } = null;
        public string UrlTemplate { get; set; }
        public string Url()
        {
            return UrlTemplate.Contains("{0}") ? string.Format(UrlTemplate, Query) : UrlTemplate;
        }
        public string Text { get; set; }
        public string Description { get; set; }
        public string NameOfView { get; set; }
        public int? Year { get; set; }

        public ValueTask<StatisticsPerYear<Smlouva.Statistics.Data>> CachedStatisticsAsync() =>
            StatisticsCache.GetSmlouvyStatisticsForQueryAsync(Query);

    }


}
