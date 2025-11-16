using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Threading.Tasks;

namespace HlidacStatu.Util
{
    public static class RealIpAddress
    {

        private class VedosNetwork : Devmasters.Net.Crawlers.CrawlerBase
        {
            public static readonly string[] initValues = new string[] {
 "45.138.104.0/23",
  "46.28.104.0/21",
  "185.181.230.232/29",
  "5.187.49.8/29",
  "31.171.155.240/29",
  "37.120.200.64/29",
  "38.165.228.184/29",
  "38.165.230.136/29",
  "38.165.231.184/29",
  "38.165.233.184/29",
  "49.128.186.40/29",
  "78.136.119.208/29",
  "78.136.119.216/29",
  "78.136.119.224/29",
  "78.136.119.232/29",
  "79.127.130.112/29",
  "79.127.168.0/29",
  "79.127.221.104/29",
  "79.127.222.8/29",
  "79.127.233.224/29",
  "79.127.235.96/29",
  "79.127.237.248/29",
  "79.127.238.168/29",
  "80.92.64.88/29",
  "81.16.239.48/29",
  "89.44.201.56/29",
  "89.111.44.248/29",
  "89.187.175.192/29",
  "89.187.175.192/28",
  "89.187.175.200/29",
  "89.187.184.136/29",
  "91.193.6.112/29",
  "93.115.0.72/29",
  "95.173.197.176/29",
  "95.174.66.136/29",
  "103.99.133.40/29",
  "103.104.75.136/29",
  "103.172.146.24/29",
  "103.182.177.32/29",
  "103.198.27.224/29",
  "109.61.81.160/29",
  "109.61.84.216/29",
  "109.235.245.40/29",
  "119.31.177.160/29",
  "138.186.141.120/29",
  "138.186.142.184/29",
  "138.199.27.0/29",
  "138.199.40.80/29",
  "146.70.19.56/29",
  "146.70.164.136/29",
  "146.70.176.176/29",
  "146.70.221.0/29",
  "150.242.141.56/29",
  "169.150.221.128/29",
  "169.150.221.128/28",
  "169.150.221.136/29",
  "169.150.230.88/29",
  "169.150.235.0/29",
  "169.150.246.16/29",
  "169.150.246.16/28",
  "169.150.246.24/29",
  "169.150.251.128/29",
  "169.150.252.232/29",
  "170.80.108.216/29",
  "170.80.109.216/29",
  "170.80.110.184/29",
  "185.24.10.208/29",
  "185.24.10.208/28",
  "185.24.10.216/29",
  "185.104.187.200/29",
  "185.162.18.96/29",
  "185.165.170.232/29",
  "190.103.177.144/29",
  "190.103.178.184/29",
  "190.103.179.192/29",
  "190.120.229.184/29",
  "190.120.230.184/29",
  "190.123.21.160/29",
  "193.29.107.72/29",
  "193.218.118.208/29",
  "195.181.174.240/29",
  "202.151.183.48/29",
  "207.211.210.64/28",
  "212.47.6.152/29",
  "212.192.218.56/29",
  "217.138.219.144/29",
  "217.138.253.184/29",
  "89.221.211.0/24",
  "2a0e:acc0::/29"
            };

            public override string Name => "VedosNetwork";

            private object lockObj = new object();
            string[] _ips = VedosNetwork.initValues;
            public override string[] IP => _ips;

            public override string[] HostNameRegex => null;

            public override string[] UserAgent => null;

            public override async Task<bool> ReloadDefinitionsFromInternetAsync()
            {
                //https://ips.wedos.global/ips.json

                lock (lockObj)
                {
                    try
                    {
                        _ = Devmasters.Net.HttpClient.Simple.GetAsync<string[]>("https://ips.wedos.global/ips.json").ContinueWith(t =>
                        {
                            if (t.Exception != null)
                            {
                                HlidacStatu.Util.Consts.LW.Error("RealIpAddress static: VedosNetwork reload failed", t.Exception);
                            }
                            else if (t.IsCompletedSuccessfully)
                            {
                                var result = t.Result;
                                if (result != null && result.Length > 0)
                                {
                                    _ips = result;
                                }
                            }
                        });

                    }
                    catch (Exception e)
                    {

                        Util.Consts.LW.Error("RealIpAddress: VedosNetwork reload failed", e);
                    }
                }
                return true;
            }
        }

        static Devmasters.Net.Crawlers.ICrawler vedos = null;
        

        static RealIpAddress()
        {
            //init vedos
            vedos = new VedosNetwork();

            _ = vedos.ReloadDefinitionsFromInternetAsync()
                .ContinueWith(t =>
                {
                    // log only on fault
                    if (t.Exception != null)
                    {
                        HlidacStatu.Util.Consts.LW.Error("RealIpAddress static: VedosNetwork reload failed", t.Exception);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);

        }

        public static bool IpFromVedos(string ip, string userAgent)
        {
            return vedos.IsItCrawler(ip, userAgent);
        }
        public static bool IpFromVedos(HttpContext httpContext)
        {
            //if on Radware, use header X-Forwarded-For
            var fromVedos = vedos.IsItCrawler(httpContext.Connection?.RemoteIpAddress?.ToString(), "");

            return fromVedos;
        }

        public static IPAddress GetIp(HttpContext httpContext)
        {
            //if on Radware, use header X-Forwarded-For

            if (httpContext?.Request?.Headers == null)
                return null;

            IPAddress.TryParse(httpContext.Request.Headers["X-Forwarded-For"], out var remoteIp);

            return remoteIp ?? httpContext.Connection?.RemoteIpAddress;
        }

    }
}
