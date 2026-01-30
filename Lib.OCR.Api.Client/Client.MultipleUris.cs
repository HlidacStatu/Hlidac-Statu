using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.OCR.Api
{
    public partial class Client
    {
        public class MultipleUris
        {
            public ProgressWithName MiningProgress { get; protected set; } = new ProgressWithName();

            IEnumerable<Uri> uris = null;
            ProgressInterval pi = null;

            public int Priority { get; protected set; }
            public string Client { get; protected set; }
            public MiningIntensity Intensity { get; protected set; }
            public TimeSpan MaxWaitingTimeOfOneFile { get; set; } = TimeSpan.FromHours(1);
            public TimeSpan? RestartTaskIn { get; set; } = null;

            public string FixedOrigFileExtension { get; set; } = "";

            decimal currProgress = 0;
            object objLock = new object();
            string apikey = null;

            public MultipleUris(string apikey, string client, int priority,
            MiningIntensity intensity, params Uri[] uris)
                : this(apikey, client, priority, intensity, uris as IEnumerable<Uri>)
            { }


            public MultipleUris(string apikey, string client, int priority,
            MiningIntensity intensity, IEnumerable<Uri> uris)
            {
                if (uris == null)
                    throw new ArgumentNullException("files");
                if (string.IsNullOrEmpty(apikey))
                    throw new ArgumentNullException("apikey");

                this.apikey = apikey;
                Client = client;
                Priority = priority;
                Intensity = intensity;
                this.uris = uris;

                if (this.uris.Count() > 0)
                    pi = new ProgressInterval(this.uris.Count());

            }

            public async Task<Dictionary<Uri, Result>> GoAsync()
            {
                List<Task<Tuple<Uri, Result>>> tas = new List<Task<Tuple<Uri, Result>>>();

                foreach (var url in uris)
                {
                    tas.Add(OneCallAsync(apikey, url));
                }

                var res = await Task.WhenAll(tas);
                return res.ToDictionary(k => k.Item1, v => v.Item2);
            }

            private void AddProgress(decimal progress, string name)
            {
                lock (objLock)
                {
                    currProgress = currProgress + progress;
                }
                MiningProgress.SetProgress(currProgress, name);
            }

            private async Task<Tuple<Uri, Result>> OneCallAsync(string apikey, Uri url)
            {
                Result res = null;
                try
                {
                    AddProgress(
                        pi.SetProgressInPercent(1).Progress
                        , url.ToString());
                    var fn = DocTools.GetFilename(url.LocalPath) + FixedOrigFileExtension;
                    _logger.Debug($"starting OCR from {url.AbsoluteUri}");
                    res = await TextFromUrlAsync(apikey, url, Client,
                            Priority, Intensity,
                            fn, MaxWaitingTimeOfOneFile,
                            RestartTaskIn);
                    _logger.Debug($"DONE OCR from {url.AbsoluteUri}");
                    AddProgress(
                        pi.SetProgressInPercent(100).Progress
                        , url.ToString());
                    return new Tuple<Uri, Result>(url, res);

                }
                catch (ApiException e)
                {
                    _logger.Error($"TextFromURLAsync {url.AbsoluteUri} API error", e);
                    return new Tuple<Uri, Result>(url, new Result() { Id = res?.Id, IsValid = Result.ResultStatus.Invalid, Error = e.ToString() });
                }
                catch (Exception e)
                {
                    _logger.Error($"TextFromURLAsync {url.AbsoluteUri} error", e);
                    return new Tuple<Uri, Result>(url, new Result() { Id = res?.Id, IsValid = Result.ResultStatus.Invalid, Error = e.ToString() });

                    //throw;
                }
            }

        }
    }
}