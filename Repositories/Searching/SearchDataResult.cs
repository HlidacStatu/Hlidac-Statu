using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HlidacStatu.Repositories.Searching
{
    public class SearchDataResult<T> : HlidacStatu.Searching.Search.ISearchResult
        where T : class
    {

        public const int DefaultPageSizeGlobal = 25;
        public virtual int DefaultPageSize() { return DefaultPageSizeGlobal; }
        public virtual int MaxResultWindow() { return Tools.MaxResultWindow; }
        public string Query { get; set; } = "";

        public HlidacStatu.Searching.RouteValues ToRouteValues(int page)
        {
            return new()
            {
                Q = Query,
                Page = page,
            };
        }


        public int Page { get; set; }
        public int PageSize { get; set; }
        public string Q
        {
            get { return Query; }
            set { Query = value; }
        }
        public string OrigQuery { get; set; }
        public bool IsValid { get; set; }
        public long Total { get; set; }
        public virtual bool HasResult { get { return IsValid && Total > 0; } }
        public string DataSource { get; set; }

        private string _order = "0";
        public string Order
        {
            get
            {
                return _order;
            }

            set
            {
                _order = string.IsNullOrWhiteSpace(value) ? "0" : value;

                if (OrderList == null)
                    InitOrderList();
                if (OrderList != null && OrderList.Count > 0)
                {
                    foreach (var item in OrderList)
                    {
                        if (item.Value == _order.ToString())
                            item.Selected = true;
                        else
                            item.Selected = false;
                    }
                }


            }
        }

        public bool ExactNumOfResults { get; set; } = false;

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> OrderList { get; set; } = null;
        public Func<T, string> AdditionalRender = null;

        public Nest.ISearchResponse<T> ElasticResults { get; set; }
        public TimeSpan ElapsedTime { get; set; } = TimeSpan.Zero;

        protected Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> orderFill = null;
        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getSmlouvyOrderList = () =>
        {
            return
                Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(SmlouvaRepo.Searching.OrderResult)).Select(
                    m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Id.ToString(), Text = "Řadit " + m.Name }
                    ).ToList();
        };

        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> emptyOrderList = () =>
        {
            return new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
        };
        public virtual string RenderQuery()
        {
            if (!string.IsNullOrEmpty(OrigQuery))
                return OrigQuery;
            else
                return Q;
        }

        public bool SmallRender { get; set; } = false;

        public SearchDataResult()
                : this(emptyOrderList)
        { }

        public SearchDataResult(Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> createdOrderList)
        {
            if (createdOrderList == null)
                createdOrderList = emptyOrderList;
            orderFill = createdOrderList;
            InitOrderList();
            PageSize = DefaultPageSize();

        }

        public void InitOrderList()
        {
            OrderList = orderFill();
        }


    }
}
