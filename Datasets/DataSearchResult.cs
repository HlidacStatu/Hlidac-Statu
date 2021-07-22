using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Searching;

namespace HlidacStatu.Datasets
{
    public class DataSearchResult
    : DataSearchResultBase<dynamic>
    {
        public DataSearchResult() : base()
        {
        }

        public DataSearchResult(NameValueCollection queryString, SmlouvaRepo.Searching.OrderResult defaultOrder = SmlouvaRepo.Searching.OrderResult.Relevance)
            : base(queryString, defaultOrder)
        {
        }

        
    }
    public class DataSearchRawResult
    : DataSearchResultBase<Tuple<string, string>>
    {

        public DataSearchRawResult() : base()
        {
        }

        public DataSearchRawResult(NameValueCollection queryString, SmlouvaRepo.Searching.OrderResult defaultOrder = SmlouvaRepo.Searching.OrderResult.Relevance)
            : base(queryString, defaultOrder)
        {
        }

    }

    public class DataSearchResultBase<T> : SearchDataResult<T>
    where T : class
    {

        public Nest.ISearchResponse<object> ElasticResultsRaw { get; set; }


        public bool ShowWatchdog { get; set; } = true;

        public IEnumerable<T> Result { get; set; }

        public DataSearchResultBase()
                : base(null)
        {
            orderFill = getDatasetOrderList;
            InitOrderList();
            Page = 1;
        }

        private DataSet _dataset = null;
        public DataSet DataSet
        {
            get
            {
                return _dataset;
            }
            set
            {
                _dataset = value;
                InitOrderList();

            }
        }


        public const string OrderAsc = " vzestupně";
        public const string OrderAscUrl = " asc";
        public const string OrderDesc = " sestupně";
        public const string OrderDescUrl = " desc";
        protected List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> getDatasetOrderList()
        {
            if (DataSet == null)
                return new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> list = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            list.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
            {
                Text = "Relevance",
                Value = "0"
            });

            for (int i = 0; i < DataSet.Registration().orderList.GetLength(0); i++)
            {
                list.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = DataSet.Registration().orderList[i, 0] + OrderAsc,
                    Value = DataSet.Registration().orderList[i, 1] + OrderAscUrl
                });
                list.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                {
                    Text = DataSet.Registration().orderList[i, 0] + OrderDesc,
                    Value = DataSet.Registration().orderList[i, 1] + OrderDescUrl
                });
            }
            return list;
                //Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(SmlouvaRepo.Search.OrderResult)).Select(
                //    m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Value, Text = "Řadit " + m.Key }
                //    ).ToList();
        }


        public DataSearchResultBase(NameValueCollection queryString,
            SmlouvaRepo.Searching.OrderResult defaultOrder = SmlouvaRepo.Searching.OrderResult.Relevance)
                : base(getSmlouvyOrderList)
        {
            int page = 1;
            int iorder = (int)defaultOrder;

            if (!string.IsNullOrEmpty(queryString["Page"]))
            {
                int.TryParse(queryString["Page"], out page);
            }
            if (Page * PageSize > MaxResultWindow())
            {
                Page = (MaxResultWindow() / PageSize) - 1;
            }
            Page = page;
            Order = iorder.ToString();
        }

        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getVZOrderList = () =>
        {
            return new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        };


        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = Q,
                Page = page,
                Order = Order,
                Id = DataSet.DatasetId
            };
        }


        public static string GetSearchUrl(string pageUrl, string Q, SmlouvaRepo.Searching.OrderResult? order = null, int? page = null)
        {

            string ret = string.Format("{0}{1}",
                pageUrl,
               GetSearchUrlQueryString(Q, order, page));

            return ret;
        }


        public static string GetSearchUrlQueryString(string Q, SmlouvaRepo.Searching.OrderResult? order = null, int? page = null)
        {

            string ret = string.Format("?Q={0}",
               System.Web.HttpUtility.UrlEncode(Q));

            if (order.HasValue)
                ret = ret + "&order=" + ((int)order.Value).ToString();
            if (page.HasValue)
                ret = ret + "&page=" + page.Value.ToString();
            return ret;
        }

        

    }
}
