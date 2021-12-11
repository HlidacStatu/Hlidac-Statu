using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class OCRServer : IHealthCheck
    {
        Devmasters.Cache.LocalMemory.AutoUpdatedCache<OCRDataStat> ocrDataStat = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<OCRDataStat>(
            TimeSpan.FromMinutes(2), "ocrDataStat_net",
            _ =>
            {
                try
                {
                    string json = "";
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent("http://ocr.hlidacstatu.cz/stats.ashx"))
                    {
                        net.Timeout = 1000 * 10;
                        net.Tries = 2;
                        json = net.GetContent().Text;
                        if (string.IsNullOrEmpty(json))
                            return new OCRDataStat();
                    }
                    var st = Newtonsoft.Json.JsonConvert.DeserializeObject<OCRDataStat>(json);
                    return st;

                }
                catch (Exception)
                {
                    return new OCRDataStat();

                }
            }
            );
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var st = ocrDataStat.Get();
                var report = $"OCR in 24 hours\n{st.queueStat.doneIn24hours} souborů, z toho {st.queueStat.imgDoneIn24hours} OCR obrázků.\n"
                    + $"{st.ocrservers.Count(m => m.created > DateTime.Now.AddMinutes(-10))} běžících OCRServerů\n"
                    + $"{st.servers.Length - 1} běžících OCR Minions ({st.servers.Where(m => m.server != "hlidacCoreOCR").Sum(m => m.doneIn24h)} OCR za 24 hod)";

                List<string> issues = new List<string>();

                bool degraded = false;
                bool bad = false;

                //degraded
                if (st.queueStat.doneIn24hours < 20000)
                {
                    issues.Add($"ERR: pouze {st.queueStat.doneIn24hours} zpracováno");
                    bad = true;
                }
                else if (st.queueStat.doneIn24hours < 50000)
                {
                    issues.Add($"Warn: pouze {st.queueStat.doneIn24hours} zpracováno");
                    degraded = true;
                }

                //degraded
                if (st.ocrservers.Count(m => m.created > DateTime.Now.AddMinutes(-10)) < 3)
                {
                    issues.Add($"ERR: pouze {st.ocrservers.Count(m => m.created > DateTime.Now.AddMinutes(-10))} běžících OCR serverů");
                    bad = true;
                }
                if (st.ocrservers.Count(m => m.created > DateTime.Now.AddMinutes(-10)) < 4)
                {
                    issues.Add($"Warn: pouze {st.ocrservers.Count(m => m.created > DateTime.Now.AddMinutes(-10))} běžících OCR serverů");
                    degraded = true;
                }

                if (bad)
                    return Task.FromResult(HealthCheckResult.Unhealthy(report + string.Join("\n", issues)));
                else if (degraded)
                    return Task.FromResult(HealthCheckResult.Degraded(report + string.Join("\n", issues)));
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
            public Queuestat queueStat { get; set; } = new Queuestat();
            public Server[] servers { get; set; } = new Server[] { };
            public Ocrserver[] ocrservers { get; set; } = new Ocrserver[] { };
            public Slowserver[] slowServers { get; set; } = new Slowserver[] { };

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
