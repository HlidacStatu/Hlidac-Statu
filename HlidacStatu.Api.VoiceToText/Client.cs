using Devmasters.Net.HttpClient;
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

        public Uri BaseApiUri { get; }
        public string ApiKey { get; }

        public Client(string apiKey)
            : this(defaultBaseUri, apiKey)
        { }
        public Client(Uri baseApiUri, string apiKey)
        {
            BaseApiUri = baseApiUri;
            ApiKey = apiKey;
        }

        public async Task<string> AddNewTaskAsync(HlidacStatu.DS.Api.Voice2Text.Options options, Uri source, string callerId, string callerTaskId, int priority)
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

                var id = await Simple.PostAsync<string>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/CreateTask",
                    form, continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
                );

                return id;
            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                if (statusCode >= 500)
                    throw new ApplicationException(e.TextContent);

                return "";
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
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
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
        public async Task<bool> CheckAsync()
        {
            try
            {   //
                var res = await Simple.GetAsync<string>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/Check?returnstatus=500", continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
                );

                return true;

            }
            catch (Devmasters.Net.HttpClient.SimpleHttpClientException e)
            {
                int statusCode = (int)e.HttpStatusNumber;
                var body = e.TextContent;
                return false;
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public async Task<HlidacStatu.DS.Api.Voice2Text.Task> GetNextTaskAsync()
        {
            HlidacStatu.DS.Api.Voice2Text.Task task = null;
            try
            {
                task = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/getnexttask", false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
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
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
                    ) ;

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
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
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

    }
}
