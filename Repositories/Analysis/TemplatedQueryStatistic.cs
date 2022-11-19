using HlidacStatu.Entities;

namespace HlidacStatu.Lib.Analysis
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
        public Analytics.StatisticsPerYear<Smlouva.Statistics.Data> Data
        {
            get
            {
                if (Year.HasValue)
                    return Repositories.Statistics.SmlouvyStatistics.CachedStatisticsForQuery(Query);
                else
                    return Repositories.Statistics.SmlouvyStatistics.CachedStatisticsForQuery(Query);
            }
        }

    }


}
