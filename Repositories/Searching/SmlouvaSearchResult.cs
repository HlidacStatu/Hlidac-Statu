using HlidacStatu.Entities;

using Microsoft.AspNetCore.Http;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Searching
{
    public class SmlouvaSearchResult
        : SearchDataResult<Smlouva>, HlidacStatu.Searching.Search.ISearchResult
    {

        public bool Chyby { get; set; } = false;

        public bool ShowWatchdog { get; set; } = true;

        public bool IncludeNeplatne { get; set; } = false;

        public IEnumerable<Smlouva> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits.Select(m => m.Source);
                else
                    return new Smlouva[] { };
            }
        }

        public SmlouvaSearchResult()
                : base(getSmlouvyOrderList)
        { }


        public SmlouvaSearchResult(IQueryCollection queryString, SmlouvaRepo.Searching.OrderResult defaultOrder = SmlouvaRepo.Searching.OrderResult.ConfidenceDesc)
                : base(getSmlouvyOrderList)
        {
            int page = 1;
            int iorder = (int)defaultOrder;

            if (!string.IsNullOrEmpty(queryString["Page"]))
            {
                int.TryParse(queryString["Page"], out page);
            }
            if (!string.IsNullOrEmpty(queryString["order"]))
            {
                int.TryParse(queryString["order"], out iorder);
            }
            if (queryString["chyby"] == "1")
            {
                Chyby = true;
            }
            if (queryString["neplatne"] == "2")
                IncludeNeplatne = true;

            if (Page * PageSize > MaxResultWindow())
            {
                Page = (MaxResultWindow() / PageSize) - 1;
            }
            Page = page;
            Order = iorder.ToString();

        }
        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = OrigQuery,
                Page = page,
                Order = Order,
                Chyby = Chyby,
            };
        }


        public static string GetSearchUrl(string pageUrl, string Q, SmlouvaRepo.Searching.OrderResult? order = null, int? page = null, bool chyby = false)
        {

            string ret = string.Format("{0}{1}",
                pageUrl,
               GetSearchUrlQueryString(Q, order, page, chyby));

            return ret;
        }


        public static string GetSearchUrlQueryString(string Q, SmlouvaRepo.Searching.OrderResult? order = null, int? page = null, bool chyby = false)
        {

            string ret = string.Format("?Q={0}",
               System.Web.HttpUtility.UrlEncode(Q));
            if (chyby)
                ret = ret + "&chyby=true";

            if (order.HasValue)
                ret = ret + "&order=" + ((int)order.Value).ToString();
            if (page.HasValue)
                ret = ret + "&page=" + page.Value.ToString();
            return ret;
        }
    }
}
