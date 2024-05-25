using Devmasters.Net.HttpClient;
using HlidacStatu.DS.Api;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HlidacStatu.Api.VoiceToText
{
    public class Client
    {
        static Uri defaultBaseUri = new Uri("https://api.hlidacstatu.cz");

        public TimeSpan TimeOut { get; private set; }
        public Uri BaseApiUri { get; }
        public string ApiKey { get; }

        public Client(string apiKey)
            : this(defaultBaseUri, apiKey, TimeSpan.FromSeconds(120))
        {
        }
        public Client(string apiKey, TimeSpan timeOut)
            : this(defaultBaseUri, apiKey, timeOut)
        {
        }
        public Client(Uri baseApiUri, string apiKey,TimeSpan timeOut)
        {
            BaseApiUri = baseApiUri;
            ApiKey = apiKey;
            TimeOut = timeOut;
        }

        public async Task<long?> AddNewTaskAsync(HlidacStatu.DS.Api.Voice2Text.Options options, 
            Uri source, string callerId, string callerTaskId, int priority,
            bool addDuplicated = false
            )
        {
            try
            {
                HlidacStatu.DS.Api.Voice2Text.Task task = new HlidacStatu.DS.Api.Voice2Text.Task()
                {
                    CallerId = callerId,
                    CallerTaskId = callerTaskId,
                    Priority = priority,
                    Source = source.ToString(),
                    SourceOptions = options
                };

                JsonContent form = JsonContent.Create<HlidacStatu.DS.Api.Voice2Text.Task>(task);

                var id = await Simple.PostAsync<long?>(
                    BaseApiUri.AbsoluteUri + $"api/v2/voice2text/CreateTask?addDuplicated={addDuplicated}",
                    form, continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }, 
                        timeout: this.TimeOut
                );

                return id;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                    throw new ApplicationException(e.TextContent);

                return 0;
            }
            catch (Exception e)
            {
                throw;
            }

        }

        public async Task<bool> TaskDoneAsync(HlidacStatu.DS.Api.Voice2Text.Task task)
        {
            try
            {
                JsonContent form = JsonContent.Create<HlidacStatu.DS.Api.Voice2Text.Task>(task);
                var res = await Simple.PostAsync<string>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/TaskDone",
                    form, continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                        timeout: this.TimeOut
                );

                return res == "OK";
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                    throw new ApplicationException(e.TextContent);

                return false;
            }
            catch (Exception e)
            {
                throw;
            }

        }
        public async Task<ApiResult> CheckAsync()
        {
            try
            {   //
                var res = await Simple.GetAsync(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/Check", continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                        timeout: this.TimeOut
                );

                return ApiResult.Ok();

            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                var body = e.TextContent;
                return ApiResult.Error(e.TextContent,statusCode);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<HlidacStatu.DS.Api.Voice2Text.Task> GetNextTaskAsync(string processEngine)
        {
            HlidacStatu.DS.Api.Voice2Text.Task task = null;
            try
            {
                task = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task>(
                    BaseApiUri.AbsoluteUri + $"api/v2/voice2text/getnexttask?processEngine={System.Net.WebUtility.UrlEncode(processEngine)}", 
                    false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                    timeout: this.TimeOut
                    );

                return task;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                {
                    throw;
                }
                else if (statusCode >= 400)
                {
                    return null;
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }


        public async Task<HlidacStatu.DS.Api.Voice2Text.Task[]> GetTasksAsync(
            int maxItems = 100, string? callerId = null, string? callerTaskId = null, HlidacStatu.DS.Api.Voice2Text.Task.CheckState? status = null)
        {
            HlidacStatu.DS.Api.Voice2Text.Task[] tasks = null;
            try
            {
                string queryStr = $"?maxitems={maxItems}";
                if (!string.IsNullOrEmpty(callerId))
                    queryStr = queryStr + $"&callerId={WebUtility.UrlEncode(callerId ?? "")}";
                if (!string.IsNullOrEmpty(callerTaskId))
                    queryStr = queryStr + $"&callerTaskId={WebUtility.UrlEncode(callerTaskId ?? "")}";
                if (status.HasValue)
                    queryStr = queryStr + $"&status={WebUtility.UrlEncode(status?.ToString() ?? "")}";

                tasks = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task[]>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/gettasks"+ queryStr, false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                        timeout: this.TimeOut
                    );

                return tasks;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                {
                    throw;
                }
                else if (statusCode >= 400)
                {
                    return null;
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<bool> SetTaskStatusAsync(long qId, HlidacStatu.DS.Api.Voice2Text.Task.CheckState status)
        {
            try
            {
                _ = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task[]>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/SetTaskStatus"
                        + $"?qid={qId}"
                        + $"&status={WebUtility.UrlEncode(status.ToString())}"
                    , false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                        timeout: this.TimeOut
                    );

                return true;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                {
                    throw;
                }
                else if (statusCode >= 400)
                {
                    return false;
                }
                else
                    return true;
            }
            catch (Exception e)
            {
                throw;
            }
        }
        public async Task<bool> RestartTaskAsync(long qId, int? withPriority = null)
        {
            try
            {
                _ = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task[]>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/SetTaskStatus"
                        + $"?qid={qId}"
                        + (withPriority.HasValue ? $"&withPriority={withPriority.Value}" : "")
                    , false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } },
                        timeout: this.TimeOut
                    ); ;

                return true;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                {
                    throw;
                }
                else if (statusCode >= 400)
                {
                    return false;
                }
                else
                    return true;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
