
using Devmasters.Cache.File;
using Hlidacstatu.Caching;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Cache;
using HlidacStatu.WebGenerator;
using Serilog;
using System;
using System.Linq;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.WebGenerator.Code
{

    public static class RemoteUrlFromWebCache
    {

        public static byte[] NoDataPicture = new byte[] { };
        private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(RemoteUrlFromWebCache));
        static RemoteUrlFromWebCache()
        {
            //Works in docker instance
            var path ="/app/";
            NoDataPicture = System.IO.File.ReadAllBytes(path + @"wwwroot/content/largetile.png");
        }

        private static readonly IFusionCache PostgreCache =
            Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSqlBinarySerializer, nameof(FirmaCache));

        private static async Task<byte[]> getAsync(KeyAndId ki)
        {
            var infoFacts = await PostgreCache.GetOrSetAsync(
                $"socialbanner_{ki.CacheNameOnDisk}",
                _ => _getBinaryDataFromUrlAsync(ki)
            );
            return infoFacts;
        }

        private static async Task invalidateAsync(KeyAndId ki)
        {
            await PostgreCache.ExpireAsync(
                $"socialbanner_{ki.CacheNameOnDisk}"
                );
        }

        private async static Task<byte[]> _getBinaryDataFromUrlAsync(KeyAndId ki)
        {

            var data = await Devmasters.Net.HttpClient.Simple.GetRawBytesAsync(
                ki.ValueForData,
                timeout: TimeSpan.FromSeconds(10));
            return data; 
        }

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
                return NoDataPicture;
            else
                return data;
        }


        public async static Task<byte[]> GetWebPageScreenshotAsync(string url, string ratio, string cacheName = null, bool refreshCache = false)
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
 
            var ki = new KeyAndId()
            {
                ValueForData = scr,
                CacheNameOnDisk = cacheName
            };
            try
            {
                if (refreshCache)
                {
                    await invalidateAsync(ki);   
                }

                byte[] data = await getAsync(ki);

                return data;
            }
            catch (Exception e)
            {
                _logger.Error(e, "WebShot GetData error");
                return NoDataPicture;
            }
        }
    }


}