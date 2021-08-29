using HlidacStatu.Entities.Insolvence;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Searching
{
    public class InsolvenceSearchResult : SearchDataResult<Rizeni>
    {
        public InsolvenceSearchResult()
            : base(getVZOrderList)
        {
        }

        public IEnumerable<Rizeni> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits.Select(m => m.Source);
                else
                    return new Rizeni[] { };
            }
        }

        public bool LimitedView { get; set; } = true;

        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = string.IsNullOrEmpty(Q) ? OrigQuery : Q,
                Page = page,
            };
        }

        public bool ShowWatchdog { get; set; } = true;


        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getVZOrderList = () =>
        {
            return
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "---" } }
                .Union(
                    Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(InsolvenceOrderResult))
                    .Select(
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Value, Text = "Řadit " + m.Key }
                    )
                )
                .ToList();
        };


        [Devmasters.Enums.ShowNiceDisplayName()]
        public enum InsolvenceOrderResult
        {
            [Devmasters.Enums.SortValue(0)]
            [Devmasters.Enums.NiceDisplayName("podle relevance")]
            Relevance = 0,

            [Devmasters.Enums.SortValue(1)]
            [Devmasters.Enums.NiceDisplayName("nově zahájené první")]
            DateAddedDesc = 1,

            [Devmasters.Enums.NiceDisplayName("nově zveřejněné poslední")]
            [Devmasters.Enums.SortValue(2)]
            DateAddedAsc = 2,


            [Devmasters.Enums.SortValue(3)]
            [Devmasters.Enums.NiceDisplayName("naposled změněné první")]
            LatestUpdateDesc = 3,

            [Devmasters.Enums.SortValue(4)]
            [Devmasters.Enums.NiceDisplayName("naposled změněné poslední")]
            LatestUpdateAsc = 43,


            [Devmasters.Enums.Disabled]
            FastestForScroll = 666

        }


    }
}
