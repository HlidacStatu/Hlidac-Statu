using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public class ClientLow
        :IAsyncDisposable, IDisposable
    {
        public string PdfUrl { get; }
        public Commands Command { get; }
        public CamelotResult.Formats Format { get; }
        public string Pages { get; }

        public enum Commands
        {
            stream,
            lattice
        }

        public string SessionId { get; private set; } = null;

        private string[] apiUrls = null;
        private bool disposedValue;

        public ClientLow(string pdfUrl, Commands command, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all")
        {
            PdfUrl = pdfUrl;
            Command = command;
            Format = format;
            Pages = pages;

            if (string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("Camelot.Service.Api")))
                throw new ArgumentException("Missing configuration key Camelot.Service.Api");
            else
                apiUrls = Devmasters.Config.GetWebConfigValue("Camelot.Service.Api").Split(";");

        }

        public async Task<ApiResult<string>> StartSessionAsync()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string url = apiUrls.First(); //TODO
                    url += "/Camelot/StartSessionWithUrl?url=" + System.Net.WebUtility.UrlEncode(this.PdfUrl);
                    url += "&command=" + this.Command.ToString().ToLower();
                    url += "&format=" + this.Format.ToString().ToLower();
                    url += "&pages=" + this.Pages;

                    var json = await wc.DownloadStringTaskAsync(new Uri(url));
                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<string>>(json);

                    this.SessionId = res.Data;
                    return res;
            }

            }
            catch (Exception e)
            {
                return new ApiResult<string>(false);
            }
        }

        public async Task<ApiResult<CamelotResult>> GetSessionAsync()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string url = Devmasters.Config.GetWebConfigValue("Camelot.Service.Api").Split(";").First(); //TODO
                    url += "/Camelot/GetSession?sessionId=" + System.Net.WebUtility.UrlEncode(this.SessionId);

                    var json = await wc.DownloadStringTaskAsync(new Uri(url));
                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<CamelotResult>>(json);

                    return res;
                }

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotResult>(false);
            }
        }

        public async Task<ApiResult> EndSessionAsync()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string url = Devmasters.Config.GetWebConfigValue("Camelot.Service.Api").Split(";").First(); //TODO
                    url += "/Camelot/EndSession?sessionId=" + System.Net.WebUtility.UrlEncode(this.SessionId);

                    var json = await wc.DownloadStringTaskAsync(new Uri(url));
                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<CamelotResult>>(json);

                    return res;
                }

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotResult>(false);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (!string.IsNullOrEmpty(this.SessionId))
                    { 
                        var res = this.EndSessionAsync().Result; 
                    }

                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ClientLow()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await this.EndSessionAsync();
            Dispose(false);
        }
    }
}
