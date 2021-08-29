using HlidacStatu.XLib.Text;

using System;
using System.Net;

namespace HlidacStatu.XLib
{
    public static class StaticData
    {
        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<WpPost[]> LastBlogPosts = null;

        static StaticData()
        {
            Util.Consts.Logger.Info("Static data - LastBlogPosts");
            LastBlogPosts = new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<WpPost[]>(
                TimeSpan.FromHours(3), (obj) =>
                {
                    try
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            string blogUrl = @"https://www.hlidacstatu.cz/texty/wp-json/wp/v2/posts";
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
        }
    }
}