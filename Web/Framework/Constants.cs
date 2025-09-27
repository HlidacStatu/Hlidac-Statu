using Microsoft.Extensions.Hosting;

using System;
using System.Linq;

namespace HlidacStatu.Web.Framework
{
    public static class Constants
    {
        public static readonly string ApiURL = "";
        public static readonly string ApiToken = "";


        public const string AntispamInputName = "zip_2";

        public const string CacheKeyName = "H_CacheKey";

        public static string[] DontIndexICOS = null;
        public static string[] DontIndexOsoby = null;

        public const string DefaultHttpClient = "default";
        
        static object lockObj = new object();
        static bool initialized = false;
        static Constants()
        {
            if (initialized == false)
                lock (lockObj)
                {
                    if (initialized == false)
                    {
                        Framework.Constants.DontIndexOsoby = Devmasters.Config
                             .GetWebConfigValue("DontIndexOsoby")
                             .Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(m => m.ToLower())
                             .ToArray();

                        Framework.Constants.DontIndexICOS = Devmasters.Config
                             .GetWebConfigValue("DontIndexFirmy")
                             .Split(new string[] { ";", "," }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(m => m.ToLower())
                             .ToArray();

                        ApiURL = Devmasters.Config.GetWebConfigValue("APIUrl");
                        
                        ApiToken = Connectors.DirectDB.Instance.GetList<string>($"select top 1 cast(token as nvarchar(40)) from AspNetUserApiTokens where id='{Devmasters.Config.GetWebConfigValue("APIKeyFromUser")}'")
                            .FirstOrDefault();

                    }
                }
        }

        public static class CachedActionLength
        {
            public static readonly TimeSpan NoCache = TimeSpan.FromSeconds(1);
            public static readonly TimeSpan Cache1H = TimeSpan.FromHours(1);
            public static readonly TimeSpan Cache2H = TimeSpan.FromHours(2);
            public static readonly TimeSpan Cache4H = TimeSpan.FromHours(4);
            public static readonly TimeSpan Cache12H = TimeSpan.FromHours(12);
            public static readonly TimeSpan Cache24H = TimeSpan.FromHours(24);
            public static readonly TimeSpan Cache48H = TimeSpan.FromHours(48);
            public static readonly TimeSpan Cache20Min = TimeSpan.FromMinutes(20);
            public static readonly TimeSpan Cache10Sec = TimeSpan.FromSeconds(10);
        }



        public static bool IsDevelopment()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

    }
}