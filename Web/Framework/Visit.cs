using HlidacStatu.Connectors;

using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Web.Framework
{
    public static class Visit
    {
        public class Crawler
        {
            public string pattern { get; set; }
            public string addition_date { get; set; }
            public string url { get; set; }
            public string[] instances { get; set; }
        }

        static object lockObj = new();
        static HashSet<string> AllCrawlers = null;
        static Visit()
        {
            lock (lockObj)
            {
                if (AllCrawlers == null)
                    AllCrawlers = new HashSet<string>(
                        Newtonsoft.Json.JsonConvert.
                            DeserializeObject<Crawler[]>(MaterializedViewsCache.GetCrawlerUserAgents())
                            .SelectMany(m => m.instances)
                            .Distinct()
                    );
            }
        }

        public static bool IsCrawler(string useragent)
        {
            if (string.IsNullOrEmpty(useragent))
                return false;

            return AllCrawlers.Contains(useragent);
        }

        public enum VisitChannel
        {
            Web = 0,
            Api = 1,
            Crawler = 2,
            Embed = 3
        }
        public static void AddVisit(string page, VisitChannel channel)
        {
            if (string.IsNullOrEmpty(page))
                return;
            DirectDB.Instance.NoResult("AddVisit", System.Data.CommandType.StoredProcedure,
                new System.Data.IDataParameter[] {
                    new Microsoft.Data.SqlClient.SqlParameter("@page", page.ToLower()),
                    new Microsoft.Data.SqlClient.SqlParameter("@date", DateTime.Now.Date),
                    new Microsoft.Data.SqlClient.SqlParameter("@channel", (int)channel)
                }
            );
        }
    }
}