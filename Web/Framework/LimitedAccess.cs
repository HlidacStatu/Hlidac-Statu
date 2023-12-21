using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework
{
    public class LimitedAccess
    {
        public class MyTestCrawlerRule : Devmasters.Net.Crawlers.CrawlerBase
        {
            public override string Name => "MyTestCrawlerRule";

            public override string[] IP => new string[] {
                //"77.93.208.131/32", "94.124.109.246/32",
                "127.0.0.1/32" };


            public override string[] UserAgent => new string[] { "Mozilla/5.0" };

            public override string[] HostNameRegex => throw new System.NotImplementedException();

            public override async Task<bool> ReloadDefinitionsFromInternetAsync()
            {
                return true;
            }
        }

        public static Devmasters.Net.Crawlers.ICrawler[] allCrawl = Devmasters.Net.Crawlers.Helper
            .AllCrawlers
            .ToArray();

        public static bool IsAuthenticatedOrSearchCrawler(HttpRequest req)
        {
            return req.HttpContext.User.Identity?.IsAuthenticated == true
                   || allCrawl.Any(cr => cr.IsItCrawler(HlidacStatu.Util.RealIpAddress.GetIp(req.HttpContext)?.ToString(), req.Headers[HeaderNames.UserAgent]));
            //return req.IsAuthenticated || MajorCrawler.Crawlers.Any(cr=>cr.Detected(req.UserHostAddress, req.UserAgent));
        }

        public static bool IsSearchCrawler(HttpRequest req)
        {
            return allCrawl.Any(cr => cr.IsItCrawler(HlidacStatu.Util.RealIpAddress.GetIp(req.HttpContext)?.ToString(), req.Headers[HeaderNames.UserAgent]));
            //return req.IsAuthenticated || MajorCrawler.Crawlers.Any(cr=>cr.Detected(req.UserHostAddress, req.UserAgent));
        }
    }
}