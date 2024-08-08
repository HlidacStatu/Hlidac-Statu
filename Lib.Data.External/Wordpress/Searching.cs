using Devmasters;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Wordpress
{
    public partial class Searching
    {

        public Uri WordpressRootUri { get; }
        public Searching(Uri wordpressRootUri)
        {
            WordpressRootUri = wordpressRootUri;
        }
        public class Result
        {
            public string Url { get; set; }
            public string Title { get; set; }
            public string Perex { get; set; }
            public DateTime Published { get; set; }
            public string ImageUrl { get; set; }
        }
        public async Task<IEnumerable<Result>> SearchAsync(string query, int page = 1, int pagesize = 5, IDictionary<string,string> customWPApiParams = null)
        {
            var raw = await SearchRawAsync(query, page, pagesize, customWPApiParams);
            if (raw?.Length>0)
            {
                var res = raw
                    .Select(m => new Result()
                    {
                        Url = m.link,
                        Title = m.title.rendered,
                        ImageUrl = "",
                        Published = m.date,
                        Perex = string.IsNullOrWhiteSpace(m.excerpt?.rendered) 
                            ? (Devmasters.TextUtil.RemoveHTML( m.content.rendered).ShortenMe(1000)) 
                            : m.excerpt.rendered,
                    })
                    .ToArray();
                return res;
            }
            else
                return Array.Empty<Result>();
        }
        public async Task<RestApiFoundItem[]> SearchRawAsync(string query, int page=1, int pagesize = 5, IDictionary<string, string> customWPApiParams = null)
        {
            try
            {
                StringBuilder customParams = new StringBuilder(1024);
                if (customWPApiParams?.Count > 0)
                {
                    foreach (var kw in customWPApiParams)
                        customParams.Append("&" + kw.Key + "=" + System.Net.WebUtility.UrlEncode(kw.Value));
                }
                string queryUrl = $"{WordpressRootUri.AbsoluteUri}wp-json/wp/v2/posts?" 
                    + $"page={page}&per_page={pagesize}&orderby=relevance" 
                    + customParams.ToString()
                    + $"&search={System.Net.WebUtility.UrlEncode(query)}";
                var res = await Devmasters.Net.HttpClient.Simple.GetAsync<RestApiFoundItem[]>(queryUrl, timeout: TimeSpan.FromSeconds(2));

                return res;
            }
            catch (Exception e)
            {
                return Array.Empty<RestApiFoundItem>();
            }
        }

    }
}
