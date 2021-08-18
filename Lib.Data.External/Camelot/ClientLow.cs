using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{

    public class ClientLow
        : IAsyncDisposable, IDisposable
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

        private bool disposedValue;

        private IApiConnection conn = null;

        private static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("Camelot.ClientLow");
        public ClientLow(IApiConnection connection,
            string pdfUrl, Commands command, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all")
        {
            PdfUrl = pdfUrl;
            Command = command;
            Format = format;
            Pages = pages;
            conn = connection;
        }

        public async Task<ApiResult<string>> StartSessionAsync(int numberOfTries = 10)
        {
            try
            {
                for (int i = 0; i < numberOfTries; i++)
                {
                    using (System.Net.WebClient wc = new System.Net.WebClient())
                    {
                        string url = conn.GetEndpointUrl();
                        url += "/Camelot/StartSessionWithUrl?url=" + System.Net.WebUtility.UrlEncode(this.PdfUrl);
                        url += "&command=" + this.Command.ToString().ToLower();
                        url += "&format=" + this.Format.ToString().ToLower();
                        url += "&pages=" + this.Pages;

                        var json = await wc.DownloadStringTaskAsync(new Uri(url));
                        var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<string>>(json);

                        if (res.ErrorCode == 0)
                        {
                            this.SessionId = res.Data;
                            return res;
                        }
                        else if (res.ErrorCode == 429)
                        {
                            Console.WriteLine("Error 429 waiting");
                            System.Threading.Thread.Sleep(1000+3*i);
                        }
                        else
                            return res;
                    }
                } //for
                return new ApiResult<string>(false);

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
                    string url = conn.GetEndpointUrl() + "/Camelot/GetSession?sessionId=" + System.Net.WebUtility.UrlEncode(this.SessionId);

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
                    string url = conn.GetEndpointUrl() + "/Camelot/EndSession?sessionId=" + System.Net.WebUtility.UrlEncode(this.SessionId);

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
        public async Task<ApiResult<CamelotVersion>> VersionAsync()
        {
            try
            {
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    string url = conn.GetEndpointUrl() + "/Camelot/Version";

                    var json = await wc.DownloadStringTaskAsync(new Uri(url));
                    var res = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<CamelotVersion>>(json);

                    return res;
                }

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotVersion>(false);
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
