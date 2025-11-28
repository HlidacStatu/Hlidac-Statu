using HlidacStatu.XLib.Text;

using System;
using System.Net;
using Serilog;

namespace HlidacStatu.XLib
{
    public static class StaticData
    {
        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<WpPost[]> LastBlogPosts = null;

        static StaticData()
        {
            Log.ForContext(typeof(StaticData)).Information("Static data - LastBlogPosts");
            Init();
        }

        static bool initialized = false;
        static void Init()
        {
            if (initialized) 
                return;
            LastBlogPosts = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<WpPost[]>(
                TimeSpan.FromHours(3), (obj) =>
                {
                    try
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            string blogUrl = @"https://texty.hlidacstatu.cz/wp-json/wp/v2/posts";
                            string response = webClient.DownloadString(blogUrl);

                            WpPost[] posts = WpPost.FromJson(response);

                            return posts;
                        }
                    }
                    catch (Exception)
                    {
                        return new WpPost[] { };
                    }
                }
            );
            initialized = true;
        }
    }
}