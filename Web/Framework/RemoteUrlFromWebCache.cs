using Devmasters.Cache.File;

using HlidacStatu.Connectors;


using System;
using System.Linq;
using Serilog;

namespace HlidacStatu.Web.Framework
{
    /// <summary>
    /// SMAZAT TODO
    /// </summary>
    public static class RemoteUrlFromWebCache
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(RemoteUrlFromWebCache));
        static RemoteUrlFromWebCache()
        {
        }

        public static volatile Devmasters.Cache.File.Manager Manager
            = Devmasters.Cache.File.Manager.GetSafeInstance("RemoteUrlFromWebCache",
                urlfn => GetBinaryDataFromUrl(urlfn),
                TimeSpan.FromHours(24 * 4));

        private static byte[] GetBinaryDataFromUrl(KeyAndId ki)
        {
            byte[] data = null;

            try
            {
                using (Devmasters.Net.HttpClient.URLContent net =
                    new Devmasters.Net.HttpClient.URLContent(ki.ValueForData))
                {
                    net.Timeout = 7000;
                    net.IgnoreHttpErrors = true;
                    data = net.GetBinary().Binary;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Manager Save error from URL {ki.ValueForData}");
            }

            if (data == null || data.Length == 0)
                return System.IO.File.ReadAllBytes(Init.WebAppRoot + @"content/icons/largetile.png");
            else
                return data;
        }


        public static byte[] GetScreenshot(string url, string ratio, string cacheName = null, bool refreshCache = false)
        {
            string[]? webShotServiceUrls = Devmasters.Config.GetWebConfigValue("WebShot.Service.Url")
                ?.Split(';')
                ?.Where(m => !string.IsNullOrEmpty(m))
                ?.ToArray();

            if (webShotServiceUrls == null || webShotServiceUrls?.Length == null || webShotServiceUrls?.Length == 0)
                webShotServiceUrls = new[] { "http://127.0.0.1:9090" };

            var webShotServiceUrl = webShotServiceUrls[Util.Consts.Rnd.Next(webShotServiceUrls.Length)];

            //string scr = webShotServiceUrl + "/png?ratio=" + rat + "&url=" + System.Net.WebUtility.UrlEncode(url);
            var sizeByRatio = ratio.Equals("1x1")
                ? "/screenshot?vp_width=1000&vp_height=1000&url="
                : "/screenshot?vp_width=1920&vp_height=1080&url=";
            
            string scr = webShotServiceUrl + sizeByRatio + System.Net.WebUtility.UrlEncode(url);
            return GetBinary(scr, cacheName, refreshCache);
        }

        public static byte[] GetBinary(string url, string cacheName = null, bool refreshCache = false)
        {
            var ki = new KeyAndId()
            {
                ValueForData = url,
                CacheNameOnDisk = cacheName
            };
            try
            {
                if (Manager.Exists(ki) == false)
                {
                    return null; //don't rebuild cache quick fix
                }
                if (refreshCache)
                {
                    Manager.Delete(ki);
                }

                byte[] data = Manager.Get(ki);

                return data;
            }
            catch (Exception e)
            {
                _logger.Error(e, "WebShot GetData error");
                return null;
            }
        }
    }
}