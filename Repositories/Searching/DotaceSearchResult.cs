﻿using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories.Searching
{
    public class DotaceSearchResult : SearchDataResult<Dotace>
    {
        public DotaceSearchResult()
            : base(getOrderList)
        {
        }

        public IEnumerable<Dotace> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits.Select(m => m.Source);
                else
                    return [];
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
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[]
                        { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "---" } }
                    .Union(
                        Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(DotaceOrderResult))
                            .Select(
                                m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem()
                                    { Value = m.Id.ToString(), Text = "Řadit " + m.Name }
                            )
                    )
                    .ToList();
        };

        public static string GetSearchUrl(string pageUrl, string Q, DotaceOrderResult? order = null, int? page = null)
        {
            return $"{pageUrl}{GetSearchUrlQueryString(Q, order, page)}";
        }


        public static string GetSearchUrlQueryString(string Q, DotaceOrderResult? order = null, int? page = null)
        {
            string ret = $"?Q={System.Web.HttpUtility.UrlEncode(Q)}";
            if (order.HasValue)
                ret = $"{ret}&order={((int)order.Value)}";
            if (page.HasValue)
                ret = $"{ret}&page={page.Value}";
            return ret;
        }


        [Devmasters.Enums.ShowNiceDisplayName()]
        public enum DotaceOrderResult
        {
            [Devmasters.Enums.SortValue(0)]
            [Devmasters.Enums.NiceDisplayName("podle relevance")]
            Relevance = 0,

            [Devmasters.Enums.SortValue(3)]
            [Devmasters.Enums.NiceDisplayName("podle roku vzniku od nejnovějších")]
            DateAddedDesc = 1,

            [Devmasters.Enums.NiceDisplayName("podle roku vzniku od nejstarších")]
            [Devmasters.Enums.SortValue(4)]
            DateAddedAsc = 2,

            [Devmasters.Enums.SortValue(5)]
            [Devmasters.Enums.NiceDisplayName("podle částky od největší")]
            AmountDesc = 3,

            [Devmasters.Enums.SortValue(6)]
            [Devmasters.Enums.NiceDisplayName("podle částky od nejmenší")]
            AmountAsc = 4,

            [Devmasters.Enums.SortValue(5)]
            [Devmasters.Enums.NiceDisplayName("podle IČO od největšího")]
            ICODesc = 5,

            [Devmasters.Enums.SortValue(6)]
            [Devmasters.Enums.NiceDisplayName("podle IČO od nejmenšího")]
            ICOAsc = 6,


            [Devmasters.Enums.Disabled]
            FastestForScroll = 666
        }
    }
}