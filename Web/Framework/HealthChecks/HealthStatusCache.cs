using HlidacStatu.Entities;
using System;

namespace HlidacStatu.Web.Framework.HealthChecks
{
    public class HealthStatusCache
    {
        internal static Devmasters.Cache.LocalMemory.AutoUpdatedCache<string> healthStatusCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<string>(TimeSpan.FromSeconds(10),
          (obj) =>
          {
              var res = Devmasters.Net.HttpClient.Simple.SharedClient(TimeSpan.FromSeconds(30))
                      .GetStringAsync("https://api.hlidacstatu.cz/health")
                      .ConfigureAwait(false)
                      .GetAwaiter().GetResult();
              return res;
          });
    }
}
