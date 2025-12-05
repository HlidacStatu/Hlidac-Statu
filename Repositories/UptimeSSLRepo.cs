using HlidacStatu.Entities;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static class UptimeSSLRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(UptimeSSLRepo));
        
        public static async Task SaveAsync(UptimeSSL item)
        {
            try
            {
                var client = Manager.GetESClient_UptimeSSL();
                await client.IndexAsync<UptimeSSL>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                _logger.Error(e, "UptimeServerRepo.SaveLastCheck error ");
                throw;
            }


        }
        public static async Task<UptimeSSL> LoadLatestAsync(string domain)
        {
            var cl = Manager.GetESClient_UptimeSSL();

            var res = await cl.SearchAsync<UptimeSSL>(s => s
                .Query(q=>q
                        .Term(t=>t.Field(f=>f.Domain).Value(domain))
                    )
                .Size(1)
                .Sort(ss=>ss.Descending(d=>d.Created))
            );
            if (res.IsValid)
            {
                if (res.Total > 0)
                    return res.Hits.First().Source;
            }

            return null;
        }


    }
}