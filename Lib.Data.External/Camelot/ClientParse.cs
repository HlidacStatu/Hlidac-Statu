using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{

    public class ClientParse
        : IDisposable
    {
        public string PdfUrl { get; }
        public ClientLow.Commands Command { get; }
        public CamelotResult.Formats Format { get; }
        public string Pages { get; }

        public string SessionId { get; private set; } = null;

        private bool disposedValue;

        private IApiConnection conn = null;
        private ClientLow cl = null;

        private static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("Camelot.ClientParse");
        public ClientParse(IApiConnection connection,
            string pdfUrl, ClientLow.Commands command, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all")
        {
            PdfUrl = pdfUrl;
            Command = command;
            Format = format;
            Pages = pages;
            conn = connection;
            cl = new ClientLow(conn.GetEndpointUrl(), connection.GetApiKey());
        }

        public async Task<ApiResult<string>> StartSessionAsync(int numberOfTries = 10)
        {
            try
            {
                for (int i = 0; i < numberOfTries; i++)
                {
                    var res = await cl.StartSessionAsync(this.PdfUrl, this.Command, this.Format, this.Pages);
                    if (res.ErrorCode == 0)
                    {
                        this.SessionId = res.Data;
                        return res;
                    }
                    else if (res.ErrorCode == 429)
                    {
                        this.SessionId = null;
                        logger.Debug($"try {i} Error 429 waiting because of {cl.ApiEndpoint}");
                        cl.Dispose();
                        cl = new ClientLow(conn.GetEndpointUrl(), conn.GetApiKey());
                        System.Threading.Thread.Sleep(200 + 3000 * i);
                    }
                    else
                    {
                        this.SessionId = null;
                        logger.Debug($"unexspected API response {cl.ApiEndpoint} {res.ErrorCode}:{res.ErrorDescription} ");

                        return res;
                    }

                } //for
                this.SessionId = null;
                logger.Error($"no free resources");
                return new ApiResult<string>(false);

            }
            catch (Exception e)
            {
                return new ApiResult<string>(false) { ErrorCode = 500, ErrorDescription = e.ToString() };
            }
        }

        public async Task<ApiResult<CamelotResult>> GetSessionAsync()
        {
            if (string.IsNullOrEmpty(this.SessionId))
                return new ApiResult<CamelotResult>(false) { ErrorCode =404, ErrorDescription ="sessionID is empty" };

            try
            {
                var res = await cl.GetSessionAsync(this.SessionId);
                return res;
            }
            catch (Exception e)
            {
                return new ApiResult<CamelotResult>(false) { ErrorDescription=e.ToString(), ErrorCode = 500 };
            }
        }

        public async Task<ApiResult> EndSessionAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(this.SessionId))
                    return new ApiResult(false) { ErrorCode = 404, ErrorDescription = "sessionID is empty" };

                return await cl.EndSessionAsync(this.SessionId);

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotResult>(false);
            }
        }
        public async Task<ApiResult<CamelotVersionData>> VersionAsync()
        {
            try
            {
                return await cl.VersionAsync();
                

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotVersionData>(false);
            }
        }
        public async Task<ApiResult<CamelotStatistics>> StatisticAsync()
        {
            try
            {
                return await cl.StatisticAsync();

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotStatistics>(false);
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
                    if (cl != null)
                        cl.Dispose();

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
            if (cl != null)
            {
                await cl.DisposeAsync();
                cl = null;
            }
            Dispose(false);
        }
    }
}
