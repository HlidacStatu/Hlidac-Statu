using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Entities.VZ;


namespace HlidacStatu.Repositories.Searching
{
    public class VerejnaZakazkaSearchData
        : SearchDataResult<VerejnaZakazka>
    {
        public VerejnaZakazkaSearchData()
        : base(getVZOrderList)
        {
            Page = 1;
            OblastiList = new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = "", Text = "---" } }
                .Union(
                    Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(VerejnaZakazkaRepo.Searching.CPVSkupiny))
                    .Select(
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Value, Text = m.Key }
                    )
                )
                .ToList();
        }

        public VerejnaZakazkaSearchData(System.Collections.Specialized.NameValueCollection queryString, VZOrderResult defaultOrder = VZOrderResult.DateAddedDesc)
                : this()
        {
            int page = 1;
            int iorder = (int)defaultOrder;
            bool Zahajeny = false;

            if (!string.IsNullOrEmpty(queryString["Page"]))
            {
                int.TryParse(queryString["Page"], out page);
            }
            if (!string.IsNullOrEmpty(queryString["order"]))
            {
                int.TryParse(queryString["order"], out iorder);
            }
            if (!string.IsNullOrEmpty(queryString["Zahajeny"]))
            {
                bool.TryParse(queryString["Zahajeny"], out Zahajeny);

            }
            Page = page;
            Order = iorder.ToString();
            this.Zahajeny = Zahajeny;



        }

        public override int DefaultPageSize()
        {
            return 50;
        }

        //public static string PouzeZahajenyQuery = "form:F01,F02,F04,F05,F07,F12,F15,F16,F17,F21,F24,CZ01,CZ02";

        [Devmasters.Enums.ShowNiceDisplayName()]
        public enum VZOrderResult
        {
            [Devmasters.Enums.SortValue(0)]
            [Devmasters.Enums.NiceDisplayName("podle relevance")]
            Relevance = 0,

            [Devmasters.Enums.SortValue(5)]
            [Devmasters.Enums.NiceDisplayName("nově zveřejněné první")]
            DateAddedDesc = 1,

            [Devmasters.Enums.NiceDisplayName("nově zveřejněné poslední")]
            [Devmasters.Enums.SortValue(6)]
            DateAddedAsc = 2,

            [Devmasters.Enums.SortValue(1)]
            [Devmasters.Enums.NiceDisplayName("nejlevnější první")]
            PriceAsc = 3,

            [Devmasters.Enums.SortValue(2)]
            [Devmasters.Enums.NiceDisplayName("nejdražší první")]
            PriceDesc = 4,

            [Devmasters.Enums.SortValue(7)]
            [Devmasters.Enums.NiceDisplayName("nově uzavřené první")]
            DateSignedDesc = 5,

            [Devmasters.Enums.NiceDisplayName("nově uzavřené poslední")]
            [Devmasters.Enums.SortValue(8)]
            DateSignedAsc = 6,

            //[Devmasters.Enums.NiceDisplayName("nejvíce chybové první")]
            //[Devmasters.Enums.SortValue(10)]
            //ConfidenceDesc = 7,

            [Devmasters.Enums.NiceDisplayName("podle odběratele")]
            [Devmasters.Enums.SortValue(98)]
            CustomerAsc = 8,
    
            [Devmasters.Enums.NiceDisplayName("podle dodavatele")]
            [Devmasters.Enums.SortValue(99)]
            ContractorAsc = 9,


            [Devmasters.Enums.Disabled]
            FastestForScroll = 666,
            [Devmasters.Enums.Disabled]
            LastUpdate = 667,
            [Devmasters.Enums.Disabled]
            PosledniZmena = 668,

        }

        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> OblastiList { get; set; } = null;
        
        private string _oblast = "";
        public string Oblast
        {
            get
            {
                return _oblast;
            }

            set
            {
                _oblast= value;
                if (OblastiList != null && OblastiList.Count > 0)
                {
                    foreach (var item in OrderList)
                    {
                        if (item.Value == _oblast)
                            item.Selected = true;
                        else
                            item.Selected = false;
                    }
                }


            }
        }
        public string[] CPV = null;

        public bool Zahajeny { get; set; } = false;

        public bool ShowWatchdog { get; set; } = true;

       public IEnumerable<VerejnaZakazka> Results
        {
            get
            {
                if (ElasticResults != null)
                    return ElasticResults.Hits.Select(m => m.Source);
                else
                    return new VerejnaZakazka[] { };
            }
        }

        protected static Func<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> getVZOrderList = () =>
        {
            return
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem[] { new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value="", Text = "---" } }
                .Union(
                    Devmasters.Enums.EnumTools.EnumToEnumerable(typeof(VZOrderResult))
                    .Select(
                        m => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem() { Value = m.Value, Text = "Řadit " + m.Key }
                    )
                )
                .ToList();
        };


        public new object ToRouteValues(int page)
        {
            return new
            {
                Q = Query,
                Page = page,
                Order = Order,
                Zahajeny = Zahajeny,
                Oblast = Oblast
            };
        }


        public static string GetSearchUrl(string pageUrl, string Q, string order = null, int? page = null, bool Zahajeny = false, string oblast = null)
        {

            string ret = string.Format("{0}{1}",
                pageUrl,
               GetSearchUrlQueryString(Q, order, page, Zahajeny,oblast));

            return ret;
        }


        public static string GetSearchUrlQueryString(string Q, string order = null, int? page = null, bool Zahajeny = false, string oblast = null)
        {
            VZOrderResult? eorder = null;

            if (Enum.TryParse<VZOrderResult>(order, out var eeorder))
                eorder = eeorder;

            string ret = string.Format("?Q={0}",
               System.Web.HttpUtility.UrlEncode(Q));
            if (Zahajeny)
                ret = ret + "&Zahajeny=true" ;

            if (eorder.HasValue)
                ret = ret + "&order=" + ((int)eorder.Value).ToString();
            if (page.HasValue)
                ret = ret + "&page=" + page.Value.ToString();
            if (oblast != null)
                ret = ret + "&oblast=" + oblast.ToString();
            return ret;
        }

    }
}
