using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities.OsobyES;

namespace HlidacStatu.Repositories.Searching
{
    public class OsobaEsSearchResult : SearchDataResult<OsobaES>
    {
        public OsobaEsSearchResult()
            : base(getOrderList)
        {
        }

       public IEnumerable<OsobaES> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits.Select(m => m.Source);
                else
                    return new OsobaES[] { };
            }
        }

        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = string.IsNullOrEmpty(Q) ? OrigQuery : Q,
                Page = page,
            };
        }

        public bool ShowWatchdog { get; set; } = true;


        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getOrderList = () =>
        {
            return
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "---" } }
                .Union(
                    Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(OsobaEsOrderResult))
                    .Select(
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Value, Text = "Řadit " + m.Key }
                    )
                )
                .ToList();
        };

        public static string GetSearchUrl(string pageUrl, string Q, OsobaEsOrderResult? order = null, int? page = null)
        {

            string ret = string.Format("{0}{1}",
                pageUrl,
               GetSearchUrlQueryString(Q, order, page));

            return ret;
        }


        public static string GetSearchUrlQueryString(string Q, OsobaEsOrderResult? order = null, int? page = null)
        {
            string ret = string.Format("?Q={0}",
               System.Web.HttpUtility.UrlEncode(Q));
            if (order.HasValue)
                ret = ret + "&order=" + ((int)order.Value).ToString();
            if (page.HasValue)
                ret = ret + "&page=" + page.Value.ToString();
            return ret;
        }


        [Devmasters.Enums.ShowNiceDisplayName()]
        public enum OsobaEsOrderResult
        {
            [Devmasters.Enums.SortValue(0)]
            [Devmasters.Enums.NiceDisplayName("podle relevance")]
            Relevance = 0,

            [Devmasters.Enums.SortValue(1)]
            [Devmasters.Enums.NiceDisplayName("podle jména vzestupně")]
            NameAsc = 1,

            [Devmasters.Enums.NiceDisplayName("podle jména sestupně")]
            [Devmasters.Enums.SortValue(2)]
            NameDesc = 2,

            [Devmasters.Enums.Disabled]
            FastestForScroll = 666

        }
    }
}
