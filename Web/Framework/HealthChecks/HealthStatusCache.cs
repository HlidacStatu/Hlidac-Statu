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
                  var res = Devmasters.Net.HttpClient.Simple.SharedClient(TimeSpan.FromSeconds(30))
                          .GetStringAsync("https://api.hlidacstatu.cz/health")
                          .ConfigureAwait(false)
                          .GetAwaiter().GetResult();
                  return res;

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
