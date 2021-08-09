using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class OCRQueue : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                string json = "";

                var ocrQueueSQL = @"select distinct t.itemtype as 'type',
		            (select count(*) from ItemToOcrQueue with (nolock) where started is null and itemtype = t.itemtype) as waiting,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null and itemtype = t.itemtype) as running,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is not null 
		            and done > DATEADD(dy,-1,getdate()) and itemtype = t.itemtype) as doneIn24H,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null 
		            and started< dateadd(hh,-24,getdate()) and itemtype = t.itemtype) as errors
		            from ItemToOcrQueue t with (nolock)
		            order by type"; ;

                var ocrQueue = HlidacStatu.Connectors.DirectDB.GetList<string, int, int, int, int>(ocrQueueSQL);

                var report = $"Current queue: {string.Join(", ", ocrQueue.Select(m=>$"{m.Item1}:waiting {m.Item2}"))}";

                List<string> issues = new List<string>();

                bool degraded = false;
                bool bad = false;


                if (ocrQueue.Where(m => m.Item1 == "VerejnaZakazka").FirstOrDefault()?.Item2 > 10000)
                {
                    issues.Add($"ERR: VerejnaZakazka Queue {ocrQueue.Where(m => m.Item1 == "VerejnaZakazka").First().Item2} done");
                    bad = true;
                }
                if (ocrQueue.Where(m => m.Item1 == "VerejnaZakazka").FirstOrDefault()?.Item2 > 6000)
                {
                    issues.Add($"Warn: VerejnaZakazka only {ocrQueue.Where(m => m.Item1 == "VerejnaZakazka").First().Item2} done");
                    degraded = true;
                }

                if (ocrQueue.Where(m => m.Item1 == "Smlouva").FirstOrDefault()?.Item2 > 6000)
                {
                    issues.Add($"ERR: Smlouvy Queue {ocrQueue.Where(m => m.Item1 == "Smlouva").First().Item2} done");
                    bad = true;
                }
                if (ocrQueue.Where(m => m.Item1 == "Smlouva").FirstOrDefault()?.Item2 > 4000)
                {
                    issues.Add($"Warn: Smlouvy only {ocrQueue.Where(m => m.Item1 == "Smlouva").First().Item2} done");
                    degraded = true;
                }

                if (ocrQueue.Where(m => m.Item1 == "Dataset").FirstOrDefault()?.Item2 > 3000)
                {
                    issues.Add($"ERR: Dataset Queue {ocrQueue.Where(m => m.Item1 == "Dataset").First().Item2} done");
                    bad = true;
                }
                if (ocrQueue.Where(m => m.Item1 == "Smlouva").FirstOrDefault()?.Item2 > 1000)
                {
                    issues.Add($"Warn: Dataset only {ocrQueue.Where(m => m.Item1 == "Dataset").First().Item2} done");
                    degraded = true;
                }

                if (bad)
                    return Task.FromResult(HealthCheckResult.Unhealthy(report + string.Join("! ", issues)));
                else if (degraded)
                    return Task.FromResult(HealthCheckResult.Degraded(report + string.Join("! ", issues)));
                else
                    return Task.FromResult(HealthCheckResult.Healthy(report));

            }
            catch (Exception e)
            {

                return Task.FromResult(HealthCheckResult.Unhealthy(exception: e));
            }

        }





        public class OCRDataStat
        {
            public Queuestat queueStat { get; set; }
            public Server[] servers { get; set; }
            public Ocrserver[] ocrservers { get; set; }
            public Slowserver[] slowServers { get; set; }

            public class Queuestat
            {
                public int processing { get; set; }
                public int inQueue { get; set; }
                public int doneIn24hours { get; set; }
                public int imgInQueue { get; set; }
                public int imgprocessing { get; set; }
                public int imgDoneIn24hours { get; set; }
                public int doneIn1hour { get; set; }
                public int imgDoneIn1hour { get; set; }
            }

            public class Server
            {
                public string server { get; set; }
                public int doneIn24h { get; set; }
            }

            public class Ocrserver
            {
                public string server { get; set; }
                public int cpu { get; set; }
                public DateTime created { get; set; }
                public int diskfree { get; set; }
                public int docsFromStart { get; set; }
                public int docsInHour { get; set; }
                public int livethreads { get; set; }
                public int memory { get; set; }
                public int ocrFromStart { get; set; }
                public int ocrInHour { get; set; }
                public int threads { get; set; }
            }

            public class Slowserver
            {
                public string server { get; set; }
                public int avgtime { get; set; }
                public int numCelkem { get; set; }
                public int numSlow { get; set; }
                public int ord { get; set; }
            }
        }


    }
}
