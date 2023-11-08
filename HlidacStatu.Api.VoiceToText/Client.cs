using Devmasters.Net.HttpClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
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
            HlidacStatu.DS.Api.Voice2Text.Task task = new HlidacStatu.DS.Api.Voice2Text.Task()
            {
                CallerId = callerId,
                CallerTaskId = callerTaskId,
                Priority = priority,
                Source = source.ToString(),
                SourceOptions = options
            };
            var json = System.Text.Json.JsonSerializer.Serialize<HlidacStatu.DS.Api.Voice2Text.Task>(task);
            JsonContent form = JsonContent.Create<HlidacStatu.DS.Api.Voice2Text.Task>(task);
            try
            {

                var id = await Simple.PostAsync<string>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/CreateTask",
                    form, continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
                );

                return id;
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                int statusCode = (int)e.StatusCode;
                if (statusCode >= 500)
                    throw new ApplicationException(e.Source);

                return "";
            }
            catch (Exception e)
            {
                throw;
            }

        }

        public async Task<bool> TaskDoneAsync(HlidacStatu.DS.Api.Voice2Text.Task task)
        {
            var json = System.Text.Json.JsonSerializer.Serialize<HlidacStatu.DS.Api.Voice2Text.Task>(task);
            JsonContent form = JsonContent.Create<HlidacStatu.DS.Api.Voice2Text.Task>(task);
            var res = await Simple.PostAsync<string>(
                BaseApiUri.AbsoluteUri + "api/v2/voice2text/TaskDone",
                form, continueOnCapturedContext: false,
                    headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
            );

            return res == "OK";
        }
        public async Task<bool> CheckAsync()
        {
            try
            {
                var res = await Simple.GetAsync<string>(
                    BaseApiUri.AbsoluteUri + "api/v2/voice2text/CreateTask?returnstatus=500", continueOnCapturedContext: false,
                        headers: new Dictionary<string, string>() { { "Authorization", this.ApiKey } }
                );

                return true;

            }
            catch (System.Net.Http.HttpRequestException e)
            {
                int statusCode = (int)e.StatusCode;

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
                task = await Simple.GetAsync<HlidacStatu.DS.Api.Voice2Text.Task>(BaseApiUri.AbsoluteUri + "api/v2/voice2text/getnexttask");

                return task;
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                int statusCode = (int)e.StatusCode;
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
    }
}
