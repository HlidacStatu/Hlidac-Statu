using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using HlidacStatu.Util;

namespace HlidacStatu.Plugin.TransparetniUcty
{
    public class CSOB2021 : BaseTransparentniUcetParser
    {
        private readonly StringComparison SC = StringComparison.Ordinal;

        string jsonUrl = "https://www.csob.cz/portal/firmy/bezne-ucty/transparentni-ucty/ucet?p_p_id=etnpwltadetail_WAR_etnpwlta&p_p_lifecycle=2&p_p_state=normal&p_p_mode=view&p_p_cacheability=cacheLevelPage&p_p_col_id=column-main&p_p_col_count=1&_etnpwltadetail_WAR_etnpwlta_ta={0}&p_p_resource_id=transactionList";
        string baseUrl = "https://www.csob.cz/portal/firmy/bezne-ucty/transparentni-ucty/ucet/-/ta/{0}";
        public override string Name => "ČSOB";

        string cisloUctuOnly = "";

        public CSOB2021(IBankovniUcet ucet) : base(ucet)
        {
            cisloUctuOnly = this.Ucet.CisloUctu.Substring(0, this.Ucet.CisloUctu.IndexOf("/"));
        }


        public class requestCommand
        {
            public Accountlist[] accountList { get; set; }
            public Filterlist[] filterList { get; set; }
            public Sortlist[] sortList { get; set; }
            public Paging paging { get; set; }

            public class Paging
            {
                public int rowsPerPage { get; set; }
                public int pageNumber { get; set; }
            }

            public class Accountlist
            {
                public long accountNumberM24 { get; set; }
            }

            public class Filterlist
            {
                public string name { get; set; }
                public string _operator { get; set; }
                public string[] valueList { get; set; }
            }

            public class Sortlist
            {
                public string name { get; set; }
                public string direction { get; set; }
                public int order { get; set; }
            }
        }


        protected override IEnumerable<IBankovniPolozka> DoParse(DateTime? fromDate = default(DateTime?), DateTime? toDate = default(DateTime?))
        {
            var statementItems = new List<IBankovniPolozka>();

            fromDate = fromDate ?? DateTime.Now.Date.AddYears(-1);
            toDate = toDate ?? DateTime.Now.Date.AddDays(1);

            using (Devmasters.Net.HttpClient.URLContent baseHtml = new Devmasters.Net.HttpClient.URLContent(string.Format(baseUrl, cisloUctuOnly)))
            {
                baseHtml.Proxy = new Devmasters.Net.Proxies.SimpleProxy("localhost", 8888);
                baseHtml.RequestParams.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*";
                var html = baseHtml.GetContent();

                for (int i = 0; i < 10000; i++)
                {
                    using (var jsonReq = new Devmasters.Net.HttpClient.URLContent(string.Format(jsonUrl, cisloUctuOnly), html.Context))
                    {
                        foreach (System.Net.Cookie coo in html.Context.Cookies)
                        {
                            if (jsonReq.RequestParams.Cookies.Any(m => m.Name == coo.Name) == false)
                                jsonReq.RequestParams.Cookies.Add(coo);
                        }
                        jsonReq.Proxy = new Devmasters.Net.Proxies.SimpleProxy("localhost", 8888);
                        jsonReq.ContentType = "application/json";
                        jsonReq.RequestParams.Accept = "application/json, text/javascript, */*; q=0.01";
                        jsonReq.RequestParams.Referer = string.Format(baseUrl, this.Ucet.CisloUctu);
                        jsonReq.RequestParams.Headers.Add("X-Requested-With", "XMLHttpRequest");
                        jsonReq.RequestParams.RawContent = Newtonsoft.Json.JsonConvert.SerializeObject(
                            new requestCommand()
                            {
                                accountList = new requestCommand.Accountlist[] { new requestCommand.Accountlist() { accountNumberM24 = Convert.ToInt64(cisloUctuOnly) } },
                                filterList = new requestCommand.Filterlist[] {
                                new requestCommand.Filterlist(){ name="AccountingDate", _operator="ge", valueList=new string[]{fromDate.Value.ToString("yyyy-MM-dd") } },
                                new requestCommand.Filterlist(){ name="AccountingDate", _operator="le", valueList=new string[]{toDate.Value.ToString("yyyy-MM-dd") } },
                             },
                                paging = new requestCommand.Paging() { pageNumber = i, rowsPerPage = 50 },
                                sortList = new requestCommand.Sortlist[] {
                                new requestCommand.Sortlist(){ direction = "DESC", name="AccountingDate",  order=1 }
                             }
                            }
                        );
                        var sjson = jsonReq.GetContent();
                    }
                }


            }




            return statementItems;
        }

    }
}
