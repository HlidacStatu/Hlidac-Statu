using System;

namespace HlidacStatu.Web.Framework.HealthChecks
{
    public class HealthStatusCache
    {
        internal static Devmasters.Cache.LocalMemory.AutoUpdatedCache<string> healthStatusCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<string>(TimeSpan.FromSeconds(10),
          (obj) =>
          {
              try
              {
                  using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent("https://api.hlidacstatu.cz/health"))
                  {
                      net.IgnoreHttpErrors = true;
                      net.Timeout = 60000;
                      net.TimeInMsBetweenTries = 50;
                      net.Tries = 3;
                      var res = net.GetContent();
                      return res.Text;
                  }

              }
              catch (Exception e)
              {
                  var res = new HlidacStatu.DS.Api.ApiResult(false)
                  {
                      ErrorCode = 500,
                      ErrorDescription = e.ToString()
                  };
                  return Newtonsoft.Json.JsonConvert.SerializeObject(res);
              }
          });
    }
}
