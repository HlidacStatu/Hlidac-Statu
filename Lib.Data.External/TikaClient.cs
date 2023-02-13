using Elastic.CommonSchema;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace HlidacStatu.Lib.Data.External
{
    public class TikaClient
    {
        static TikaClient()
        {
            int threads;
            if (!int.TryParse(Devmasters.Config.GetWebConfigValue("Tika.Threads"), out threads))
                threads = 5;
            sem = new Semaphore(threads, threads, "HlidacStatu.Lib.OCR.ExtractTika");
        }

        private static Semaphore sem = null;

        public static ExtractionTextResult GetText(string filename)
        {
            try
            {
                _ = sem.WaitOne();
                return new TikaClient(filename).Extract();
            }
            finally
            {
                _ = sem.Release();
            }


        }



        private class Endpoints
        {
            static string[] defaultServers = Devmasters.Config.GetWebConfigValue("Tika").Split(','); //new string[] { "http://217.31.202.164:9998", "http://moonlake.hlidacstatu.cz:9998" };

            public static Random rnd = new Random();
            public Dictionary<string, DateTime> Servers { get; }

            System.Timers.Timer timer = new System.Timers.Timer(5000);

            public Endpoints()
                : this(defaultServers)
            { }
            public Endpoints(string servers)
                : this(servers?.Split(',', ';'))
            { }

            public Endpoints(string[] initServers)
            {
                Servers = (initServers ?? defaultServers)
                    .ToDictionary(k => k, v => DateTime.Now.AddHours(-5));

                timer.AutoReset = false;
                timer.Elapsed += Timer_Elapsed;
                Timer_Elapsed(null, null);
                //timer.Start();
            }

            private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                timer.Stop();
                foreach (var key in Servers.Keys.ToArray())
                {
                    try
                    {

                        var s = Devmasters.Net.HttpClient.Simple.GetAsync(key + "/tika", timeout: TimeSpan.FromSeconds(5))
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                        if (!string.IsNullOrEmpty(s) && s.Contains("Tika"))
                            Servers[key] = DateTime.Now;

                    }
                    catch (Exception)
                    {
                    }
                }

                timer.Start();
            }

        }

        static Endpoints instanceEndpoint = new Endpoints();
        static string GetServer()
        {
            string[] validS = instanceEndpoint.Servers
                .Where(m => (DateTime.Now - m.Value).TotalSeconds < 60)
                .Select(m => m.Key)
                .ToArray();
            if (validS.Length == 0)
                return instanceEndpoint.Servers.First().Key;

            return validS[Endpoints.rnd.Next(validS.Length)];

        }

        static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("HlidacStatu.Lib.OCR.TikaServer");

        string[] skip = new string[] { "Content-Type", "X-TIKA:content" };
        public string Filename { get; }
        public int Tries { get; set; } = 3;

        private TikaClient(string filename)
        {
            Filename = filename;
        }

        public ExtractionTextResult Extract()
        {
            if (System.IO.File.Exists(Filename) == false)
            {
                logger.Warning($"TIKA SERVER: file {Filename} doesn't exists ");
                return null;
            }
            int numOfTries = 0;
            numOfTries++;
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();

            try
            {

                do
                {
                    try
                    {
                        string serverUrl = GetServer() + "/rmeta/text";

                        var client = Devmasters.Net.HttpClient.Simple.SharedClient(TimeSpan.FromMinutes(3));
                        var byteArrayContent = new ByteArrayContent(System.IO.File.ReadAllBytes(Filename));
                        var data = client.PutAsync(serverUrl, byteArrayContent)
                            .Result
                            .Content.ReadAsByteArrayAsync()
                            .Result;

                        if (data == null)
                        {
                            numOfTries++;
                            System.Threading.Thread.Sleep(1000);
                        }
                        else
                        {
                            var txt = System.Text.Encoding.UTF8.GetString(data);
                            Newtonsoft.Json.Linq.JArray json = Newtonsoft.Json.Linq.JArray.Parse(txt);
                            if (json.Count > 0)
                            {
                                ExtractionTextResult res = new ExtractionTextResult();
                                res.ContentType = json[0].Value<string>("Content-Type");
                                res.Text = json[0].Value<string>("X-TIKA:content");
                                res.Metadata = json[0].Children()
                                    .Select(m => m as JProperty)
                                    .Where(m => m != null)
                                    .Where(m => skip.Contains(m.Name) == false)
                                    .ToDictionary(k => k.Name, v => v.Value.ToString());

                                return res;
                            }

                        }


                    }
                    catch (Exception e)
                    {
                        logger.Error("Tika server failed", e);
                        numOfTries++;
                        System.Threading.Thread.Sleep(1000);
                    }
                } while (numOfTries <= Tries);
            }
            finally
            {
                sw.Stop();
                logger.Info($"TIKA SERVER: parsing file {Filename} done in {sw.ElapsedMilliseconds:### ###}ms ");
            }
            return null;
        }

        private class PatientWebClient : System.Net.WebClient
        {
            public int Timeout { get; set; }
            public PatientWebClient() : this(60 * 1000) { } //1min

            public PatientWebClient(int timeoutInMs)
            {
                this.Timeout = timeoutInMs;
                this.Encoding = System.Text.Encoding.UTF8;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {

                HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
                request.ContentType = "text/xml";
                request.Accept = "";
                if (request != null)
                {
                    ((HttpWebRequest)request).KeepAlive = false;
                    ((HttpWebRequest)request).ReadWriteTimeout = Timeout;
                    request.Timeout = this.Timeout;
                }
                return request;
            }

        }
        public class ExtractionTextResult
        {

            public string Text { get; set; }
            public string ContentType { get; set; }
            public IDictionary<string, string> Metadata { get; set; }

        }

    }
}
