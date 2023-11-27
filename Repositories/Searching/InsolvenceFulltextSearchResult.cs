using HlidacStatu.Entities.Insolvence;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Searching
{
    public class InsolvenceFulltextSearchResult : SearchDataResult<SearchableDocument>
    {

        public InsolvenceFulltextSearchResult(InsolvenceSearchResult query)
        {
            this.DataSource = query.DataSource;
            this.Order = query.Order;
            this.OrigQuery = query.OrigQuery;
            this.Page = query.Page;
            this.PageSize = query.PageSize;
            this.LimitedView = query.LimitedView;   
            this.Q = query.Q;
            this.Query = query.Query;
            this.OrderList = query.OrderList;
            this.ShowWatchdog = query.ShowWatchdog;
            this.SmallRender = query.SmallRender;            

        }

        public InsolvenceFulltextSearchResult()
            : base(getVZOrderList)
        {
        }

        public IEnumerable<SearchableDocument> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits
                        .Select(m => m.InnerHits["rec"].Documents<SearchableDocument>().FirstOrDefault())
                        .Where(m => m != null)
                        ;
                else
                    return new SearchableDocument[] { };
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
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Id.ToString(), Text = "Řadit " + m.Name }
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


        public static SortDescriptor<SearchableDocument> GetSort(string sorder)
        {
            InsolvenceSearchResult.InsolvenceOrderResult order = InsolvenceSearchResult.InsolvenceOrderResult
                .Relevance;
            Enum.TryParse<InsolvenceSearchResult.InsolvenceOrderResult>(sorder, out order);
            return GetSort(order);
        }

        public static SortDescriptor<SearchableDocument> GetSort(InsolvenceSearchResult.InsolvenceOrderResult order)
        {
            SortDescriptor<SearchableDocument> s = new SortDescriptor<SearchableDocument>().Field(f => f.Field("_score").Descending());
            switch (order)
            {
                case InsolvenceSearchResult.InsolvenceOrderResult.DateAddedDesc:
                    s = new SortDescriptor<SearchableDocument>().Field(m => m.Field(f => f.Rizeni.DatumZalozeni).Descending());
                    break;
                case InsolvenceSearchResult.InsolvenceOrderResult.DateAddedAsc:
                    s = new SortDescriptor<SearchableDocument>().Field(m => m.Field(f => f.Rizeni.DatumZalozeni).Ascending());
                    break;
                case InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc:
                    s = new SortDescriptor<SearchableDocument>().Field(m => m.Field(f => f.Rizeni.PosledniZmena).Descending());
                    break;
                case InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateAsc:
                    s = new SortDescriptor<SearchableDocument>().Field(m => m.Field(f => f.Rizeni.PosledniZmena).Ascending());
                    break;
                case InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll:
                    s = new SortDescriptor<SearchableDocument>().Field(f => f.Field("_doc"));
                    break;
                case InsolvenceSearchResult.InsolvenceOrderResult.Relevance:
                default:
                    break;
            }

            return s;
        }



    }

}
