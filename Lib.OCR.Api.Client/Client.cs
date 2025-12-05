using Newtonsoft.Json.Linq;

using System;
using System.Threading.Tasks;
using HlidacStatu.DS.Api;
using Serilog;

namespace HlidacStatu.Lib.OCR.Api
{
    public partial class Client
    {

        const string defaultApiUrl = "https://ocr.hlidacstatu.cz/";
        static string ApiUrl = defaultApiUrl;

        static Client()
        {
            if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("OCR.ApiUrl")))
                ApiUrl = Devmasters.Config.GetWebConfigValue("OCR.ApiUrl");
        }

        public static void SetCustomApiURL(string url)
        {
            ApiUrl = url;
        }
        public static void SetDefaultApiURL()
        {
            SetCustomApiURL(defaultApiUrl);
        }

        public enum MiningIntensity
        {
            ForceOCR = -1,
            Maximum = 0,
            SkipOCR = 1
        }

        private readonly static ILogger _logger = Log.ForContext<Client>();

        public static TimeSpan defaultWaitingTime = TimeSpan.FromHours(1);

        #region with OcrWork.TaskPriority


        public static async Task<Result> TextFromUrlAsync(string apikey, Uri url, string client, OcrWork.TaskPriority priority,
            MiningIntensity intensity, string origFilename = null, TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            return await TextFromUrlAsync(apikey, url, client, (int)priority, intensity, origFilename, maxWaitingTime, restartTaskAfterTime/*, callBackData */);
        }
        public static async Task<Result> TextFromFileAsync(string apikey, string fileOnDisk, string client, OcrWork.TaskPriority priority,
            MiningIntensity intensity, string origFilename = "", TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            return await TextFromFileAsync(apikey, fileOnDisk, client, (int)priority, intensity, origFilename, maxWaitingTime, restartTaskAfterTime/*, callBackData */);
        }

        public static Result TextFromFile(string apikey, string fileOnDisk, string client, OcrWork.TaskPriority priority,
            MiningIntensity intensity, string origFilename = "", TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            return TextFromFile(apikey, fileOnDisk, client, (int)priority, intensity, origFilename, maxWaitingTime, restartTaskAfterTime/*, callBackData */);
        }

        public static Result TextFromUrl(string apikey, Uri url, string client, OcrWork.TaskPriority priority,
            MiningIntensity intensity, string origFilename = null, TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            return TextFromUrl(apikey, url, client, (int)priority, intensity, origFilename, maxWaitingTime, restartTaskAfterTime/*, callBackData */);
        }

        #endregion


        public static async Task<Result> TextFromUrlAsync(string apikey, Uri url, string client, int priority,
            MiningIntensity intensity, string origFilename = null, TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null /*, Api.CallbackData callBackData = null*/)
        {
            var tmpFile = TempIO.GetTemporaryFilename();
            try
            {
                using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url.AbsoluteUri))
                {
                    net.TimeInMsBetweenTries = 2000;
                    net.Timeout = 60000;
                    net.Tries = 5;
                    net.IgnoreHttpErrors = true;
                    await System.IO.File.WriteAllBytesAsync(tmpFile, net.GetBinary().Binary);
                }
                return await TextFromFileAsync(apikey, tmpFile, client, priority,
                    intensity, origFilename, maxWaitingTime, restartTaskAfterTime);
            }
            catch (Exception e)
            {
                throw new ApiException("exception API TextFromFileAsync  ", e);
                //return new Result() { Id = taskId, IsValid = Result.ResultStatus.Invalid, Error = json["error"].Value<string>() };
            }
            finally
            {
                TempIO.DeleteFile(tmpFile);
            }

        }

        public static async Task<Result> TextFromFileAsync(string apikey, string fileOnDisk, string client, int priority,
            MiningIntensity intensity, string origFilename = "", TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            CallbackData callBackData = null; //temporaty disable callBack

            if (string.IsNullOrEmpty(origFilename))
                origFilename = System.IO.Path.GetFileName(fileOnDisk);

            var id = await uploadFileAsync(apikey, fileOnDisk, client, priority, intensity, origFilename,
                maxWaitingTime ?? defaultWaitingTime, restartTaskAfterTime/*, callBackData */);
            ///await Task.Delay()

            if (callBackData == null)
                return await WaitingForResultAsync(apikey, id, maxWaitingTime ?? defaultWaitingTime);
            else
                return new Result() { Id = id, IsValid = Result.ResultStatus.InQueueWithCallback };
            //return result;
        }

        public static Result TextFromFile(string apikey, string fileOnDisk, string client, int priority,
            MiningIntensity intensity, string origFilename = "", TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            CallbackData callBackData = null; //temporaty disable callBack

            if (string.IsNullOrEmpty(origFilename))
                origFilename = DocTools.GetFilename(fileOnDisk);

            TimeSpan? waitTime = maxWaitingTime;
            if (waitTime == null && callBackData != null)
                waitTime = TimeSpan.FromDays(14);
            else if (waitTime == null)
                waitTime = defaultWaitingTime;

            var id = uploadFile(apikey, fileOnDisk, client, priority, intensity, origFilename,
            maxWaitingTime ?? defaultWaitingTime, restartTaskAfterTime/*, callBackData */);

            if (callBackData == null)
                return WaitingForResult(apikey, id, maxWaitingTime ?? defaultWaitingTime);
            else
                return new Result() { Id = id, IsValid = Result.ResultStatus.InQueueWithCallback };
        }


        public static Result TextFromUrl(string apikey, Uri url, string client, int priority,
           MiningIntensity intensity, string origFilename = null, TimeSpan? maxWaitingTime = null,
           TimeSpan? restartTaskAfterTime = null /*, Api.CallbackData callBackData = null*/)
        {
            var tmpFile = TempIO.GetTemporaryFilename();
            try
            {
                using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url.AbsoluteUri))
                {
                    net.TimeInMsBetweenTries = 2000;
                    net.Timeout = 60000;
                    net.Tries = 5;
                    net.IgnoreHttpErrors = true;
                    System.IO.File.WriteAllBytes(tmpFile, net.GetBinary().Binary);
                }
                return TextFromFile(apikey, tmpFile, client, priority,
                    intensity, origFilename, maxWaitingTime, restartTaskAfterTime);
            }
            catch (Exception e)
            {
                throw new ApiException("exception API TextFromFileAsync  ", e);
                //return new Result() { Id = taskId, IsValid = Result.ResultStatus.Invalid, Error = json["error"].Value<string>() };
            }
            finally
            {
                TempIO.DeleteFile(tmpFile);
            }

        }

        public static Result TextFromUrl_old(string apikey, Uri url, string client, int priority,
            MiningIntensity intensity, string origFilename = null, TimeSpan? maxWaitingTime = null,
            TimeSpan? restartTaskAfterTime = null/*, Api.CallbackData callBackData = null*/)
        {
            CallbackData callBackData = null; //temporaty disable callBack

            string fullUrl = null;
            string taskId = null;
            byte[] resbyte;
            string res = "";
            try
            {
                if (string.IsNullOrEmpty(origFilename))
                {
                    origFilename = DocTools.GetFilename(url.LocalPath);
                }
                TimeSpan? waitTime = maxWaitingTime;
                if (waitTime == null && callBackData != null)
                    waitTime = TimeSpan.FromDays(14);
                else if (waitTime == null)
                    waitTime = defaultWaitingTime;

                string callBackDataString = "";
                if (callBackData != null)
                    callBackDataString = Newtonsoft.Json.JsonConvert.SerializeObject(callBackData);

                using (WebOcr wc = new WebOcr())
                {
                    string param = "url=" + System.Net.WebUtility.UrlEncode(url.AbsoluteUri)
                        + "&apikey=" + apikey
                        + "&fn=" + System.Net.WebUtility.UrlEncode(origFilename ?? "")
                        + "&client=" + System.Net.WebUtility.UrlEncode(client ?? "")
                        + "&priority=" + priority
                        + "&intensity=" + (int)intensity
                        + "&expirationIn=" + (int)(waitTime.Value.TotalSeconds * 1.05)  //add 5%
                        + "&restartIn=" + (int)(restartTaskAfterTime?.TotalSeconds ?? 0)
                        + "&callbackData=" + System.Net.WebUtility.UrlEncode(callBackDataString);

                    fullUrl = ApiUrl + "addTask.ashx?" + param;
                    resbyte = wc.DownloadData(fullUrl);
                    res = System.Text.Encoding.UTF8.GetString(resbyte);
                    JToken json = JToken.Parse(res);

                    if (json["taskid"] != null)
                    {
                        taskId = json["taskid"].ToString();
                    }
                    else
                    {
                        _logger.Error($"ExtApi.TextFromUrlAsync API Exception\nUrl:{url.AbsoluteUri}\n content: " + res);
                        return new Result() { Id = taskId, IsValid = Result.ResultStatus.Invalid, Error = json["error"].Value<string>() };
                    }

                }

                if (callBackData == null)
                    return WaitingForResult(apikey, taskId, maxWaitingTime ?? defaultWaitingTime);
                else
                    return new Result() { Id = taskId, IsValid = Result.ResultStatus.InQueueWithCallback };


            }
            catch (System.Net.WebException e)
            {
                _logger.Debug(e, $"called ext API TextFromFile {fullUrl}.\nResponse:{res} \n" + ApiUrl);
                throw new ApiException("called ext API ", e);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"exception API TextFromFile {fullUrl}.\nResponse: {res}\n" + ApiUrl);
                throw new ApiException("exception API TextFromFile  ", e);
            }
            finally
            {
                //TempIO.DeleteFile(tmpFile);
            }

        }

        private static string uploadFile(string apikey, string fileOnDisk, string client, int priority,
            MiningIntensity intensity, string origFilename, TimeSpan maxWaitingTime, TimeSpan? restartTaskAfterTime = null,
            CallbackData callBackData = null
            )
        {
            string callBackDataString = "";
            if (callBackData != null)
                callBackDataString = Newtonsoft.Json.JsonConvert.SerializeObject(callBackData);

            var fullUrl = "";
            byte[] resbyte;
            string res = "";
            try
            {
                using (WebOcr wc = new WebOcr())
                {
                    string param = "fn=" + System.Net.WebUtility.UrlEncode((origFilename ?? ""))
                        + "&client=" + System.Net.WebUtility.UrlEncode(client ?? "")
                        + "&apikey=" + apikey
                        + "&priority=" + priority
                        + "&intensity=" + (int)intensity
                        + "&expirationIn=" + (int)(maxWaitingTime.TotalSeconds * 1.05)  //add 5%
                        + "&restartIn=" + (int)(restartTaskAfterTime?.TotalSeconds ?? 0)
                        + "&callbackData=" + System.Net.WebUtility.UrlEncode(callBackDataString);


                    _logger.Debug("Uploading file for OCR " + "addTask.ashx?" + param);
                    fullUrl = ApiUrl + "addTask.ashx?" + param;
                    resbyte = wc.UploadFile(fullUrl, "POST", fileOnDisk);
                    _logger.Debug("Uploaded file for OCR " + "addTask.ashx?" + param);

                    res = System.Text.Encoding.UTF8.GetString(resbyte);
                    JToken json = JToken.Parse(res);

                    if (json["taskid"] != null)
                    {
                        _logger.Debug("Uploaded into task " + json["taskid"].ToString() + " file for OCR " + "addTask.ashx?" + param);
                        return json["taskid"].ToString();
                    }
                    else
                    {
                        _logger.Error($"ExtApi.TextFromFile API Exception\nFile:{origFilename}\n content: " + res);
                        throw new ApiException(res);
                    }
                }
            }
            catch (System.Net.WebException e)
            {
                _logger.Debug(e, $"called ext API TextFromFile {fullUrl}.\nResponse: {res}\n" + ApiUrl);
                throw new ApiException("called ext API ", e);
            }
            catch (Exception e)
            {
                _logger.Debug(e, $"called ext API TextFromFile {fullUrl}.\nResponse: {res}\n" + ApiUrl);
                throw;
            }
        }


        private static async Task<string> uploadFileAsync(string apikey, string fileOnDisk, string client, int priority,
            MiningIntensity intensity, string origFilename, TimeSpan maxWaitingTime, TimeSpan? restartTaskAfterTime = null,
            CallbackData callBackData = null
            )
        {
            string callBackDataString = "";
            if (callBackData != null)
                callBackDataString = Newtonsoft.Json.JsonConvert.SerializeObject(callBackData);

            var fullUrl = "";
            byte[] resbyte;
            string res = "";
            try
            {
                using (WebOcr wc = new WebOcr())
                {
                    string param = "fn=" + System.Net.WebUtility.UrlEncode((origFilename ?? ""))
                        + "&client=" + System.Net.WebUtility.UrlEncode(client ?? "")
                        + "&apikey=" + apikey
                        + "&priority=" + priority
                        + "&intensity=" + (int)intensity
                        + "&expirationIn=" + (int)(maxWaitingTime.TotalSeconds * 1.05)  //add 5%
                        + "&restartIn=" + (int)(restartTaskAfterTime?.TotalSeconds ?? 0)
                        + "&callbackData=" + System.Net.WebUtility.UrlEncode(callBackDataString);

                    fullUrl = ApiUrl + "addTask.ashx?" + param;
                    resbyte = await wc.UploadFileTaskAsync(fullUrl, "POST", fileOnDisk);

                    res = System.Text.Encoding.UTF8.GetString(resbyte);
                    JToken json = JToken.Parse(res);

                    if (json["taskid"] != null)
                    {
                        return json["taskid"].ToString();
                    }
                    else
                    {
                        _logger.Error($"ExtApi.TextFromFileAsync API Exception\nFile:{origFilename}\n content: " + res);
                        throw new ApiException(res);
                    }
                }
            }
            catch (System.Net.WebException e)
            {
                _logger.Debug(e, $"called ext API TextFromFile {fullUrl}.\nResponse: {res}\n" + ApiUrl);
                throw new ApiException("called ext API ", e);
            }
            catch (Exception e)
            {
                _logger.Debug(e, $"called ext API TextFromFile {fullUrl}.\nResponse: {res}\n" + ApiUrl);
                throw;
            }
        }


        private static Result WaitingForResult(string apikey, string taskid)
        {
            return WaitingForResult(apikey, taskid, TimeSpan.FromHours(2));
        }

        static int longTimeThreshold = 10000;
        static int longWaitingTimesForResult = 10000;
        static int shortgWaitingTimesForResult = 3000;

        private static Result WaitingForResult(string apikey, string taskid, TimeSpan maxWaitingTime)
        {
            //queued 
            DateTime requestSent = DateTime.Now;
            bool done = false;
            do
            {
                TimeSpan waiting = DateTime.Now - requestSent;
                string data = null;
                try
                {

                    using (WebOcr wc = new WebOcr(60 * 1000))
                    {
                        data = wc.DownloadString(ApiUrl + $"result.ashx?apikey={apikey}&taskid={taskid}");
                    }
                }
                catch (System.Net.WebException ex)
                {
                    throw new ApiException("WaitingForResult  web exc", ex);
                }
                TaskStatus status = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskStatus>(data);
                if (status.Status == TaskStatus.CurrentStatus.NotFound)
                    return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "not found", Started = requestSent, Ends = DateTime.Now };
                else if (status.Status == TaskStatus.CurrentStatus.Done)
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(status.Result);

                if (waiting > maxWaitingTime)
                    return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "timeout", Started = requestSent, Ends = DateTime.Now };


                if (waiting.TotalMilliseconds > longTimeThreshold)
                    System.Threading.Thread.Sleep(longWaitingTimesForResult);
                else
                    System.Threading.Thread.Sleep(shortgWaitingTimesForResult);


            } while (done == false);

            return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "timeout", Started = requestSent, Ends = DateTime.Now };

        }

        private static async Task<Result> WaitingForResultAsync(string apikey, string taskid)
        {
            return await WaitingForResultAsync(apikey, taskid, TimeSpan.FromHours(2));
        }


        private static async Task<Result> WaitingForResultAsync(string apikey, string taskid, TimeSpan maxWaitingTime)
        {
            //queued 
            DateTime requestSent = DateTime.Now;
            bool done = false;
            do
            {
                TimeSpan waiting = DateTime.Now - requestSent;
                string data = null;
                try
                {

                    using (WebOcr wc = new WebOcr(60 * 1000))
                    {
                        data = await wc.DownloadStringTaskAsync(ApiUrl + $"result.ashx?apikey={apikey}&taskid={taskid}");
                    }
                }
                catch (System.Net.WebException ex)
                {
                    throw new ApiException("WaitingForResultAsync web exc", ex);
                }
                TaskStatus status = Newtonsoft.Json.JsonConvert.DeserializeObject<TaskStatus>(data);
                if (status.Status == TaskStatus.CurrentStatus.NotFound)
                    return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "not found", Started = requestSent, Ends = DateTime.Now };
                else if (status.Status == TaskStatus.CurrentStatus.Done)
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Result>(status.Result);
                else
                {
                    if (waiting > maxWaitingTime)
                        return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "timeout", Started = requestSent, Ends = DateTime.Now };

                    else if (waiting.TotalMilliseconds > longTimeThreshold)
                        await Task.Delay(longWaitingTimesForResult);
                    else
                        await Task.Delay(shortgWaitingTimesForResult);

                }
            } while (done == false);

            return new Result() { Id = taskid, IsValid = Result.ResultStatus.Invalid, Error = "timeout", Started = requestSent, Ends = DateTime.Now };

        }

        public static bool PingExternal(string url)
        {
            var pingStruct = new { server = "" };
            try
            {
                using (WebOcr wc = new WebOcr(30000))
                {
                    var json = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(wc.DownloadString(url + "ping.ashx"), pingStruct);
                    return json.server == "OK";
                }

            }
            catch (Exception)
            {
                return false;
            }
        }



    }
}
