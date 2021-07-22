using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.OCR.Api
{
    public partial class Client
    {
        public class MultipleFiles
        {
            public ProgressWithName MiningProgress { get; protected set; } = new ProgressWithName();

            IEnumerable<string> files = null;
            ProgressInterval pi = null;

            public string ParentTaskId { get; set; } = null;
            public int Priority { get; protected set; }
            public string Client { get; protected set; }
            public MiningIntensity Intensity { get; protected set; }
            public TimeSpan MaxWaitingTimeOfOneFile { get; set; } = TimeSpan.FromHours(1);
            public TimeSpan? RestartTaskIn { get; set; } = null;

            string Apikey=null;
            decimal currProgress = 0;
            object objLock = new object();

            public MultipleFiles(string apikey, string client, int priority,
            MiningIntensity intensity, params string[] files)
                : this(apikey, client, priority, intensity, files as IEnumerable<string>)
            { }


            public MultipleFiles(string apikey, string client, int priority,
            MiningIntensity intensity, IEnumerable<string> files)
            {
                if (files == null)
                    throw new ArgumentNullException("files");
                if (string.IsNullOrEmpty( apikey ))
                    throw new ArgumentNullException("apikey");

                Apikey = apikey;
                Client = client;
                Priority = priority;
                Intensity = intensity;
                this.files = files;

                if (this.files.Count()>0)
                    pi = new ProgressInterval(this.files.Count());

            }

            public async Task<Result[]> Go()
            {
                List<Task<Result>> tas = new List<Task<Result>>();
                
                logger.Debug($"MultipleFiles starting for {files.Count()} files for parentTaskId:{ParentTaskId ?? ""}");

                foreach (var fn in files)
                {
                    tas.Add(OneCall(Apikey, fn));
                }

                logger.Debug($"MultipleFiles waiting for {files.Count()} files for parentTaskId:{ParentTaskId ?? ""}");
                var res = await Task.WhenAll(tas);
                logger.Debug($"MultipleFiles done for {files.Count()} files for parentTaskId:{ParentTaskId ?? ""}");
                return res;
            }

            private void AddProgress(decimal progress, string name)
            {
                lock (objLock)
                {
                    currProgress = currProgress + progress;
                }
                MiningProgress.SetProgress(currProgress, name);
            }

            private async Task<Result> OneCall(string apikey, string fn)
            {
                Result res = null;
                try
                {
                    AddProgress(
                        pi.SetProgressInPercent(1).Progress
                        , fn);
                    logger.Debug($"Starting Api.Client.TextFromFileAsync for {fn}  parentTaskId:{ParentTaskId ?? ""}");
                    res = await TextFromFileAsync(apikey, fn, Client,
                            Priority, Intensity,
                            System.IO.Path.GetFileName(fn), MaxWaitingTimeOfOneFile,
                            RestartTaskIn);
                    logger.Debug($"Done Api.Client.TextFromFileAsync resultOK:{res.IsValid} fromOCR:{res.Server}  for {fn}  parentTaskId:{ParentTaskId ?? ""}");
                    AddProgress(
                        pi.SetProgressInPercent(100).Progress
                        , fn);
                    return res;

                }
                catch (ApiException e)
                {
                    logger.Error($"TextFromFileAsync error for {fn}  parentTaskId:{ParentTaskId ?? ""}", e);
                    return new Result() { Id=res?.Id,  IsValid = Result.ResultStatus.Invalid, Error = e.ToString() };
                }
                catch (Exception e)
                {
                    logger.Error($"TextFromFileAsync error for {fn}  parentTaskId:{ParentTaskId ?? ""}", e);
                    return new Result() { Id = res?.Id, IsValid = Result.ResultStatus.Invalid, Error = e.ToString() };

                    //throw;
                }
            }

        }
    }
}