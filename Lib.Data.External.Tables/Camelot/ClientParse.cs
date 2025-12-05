using HlidacStatu.DS.Api;
using System;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{

    public class ClientParse
        : IDisposable
    {
        public string PdfUrl { get; }
        public ClientLow.Commands Command { get; }
        public HlidacStatu.DS.Api.TablesInDoc.Formats Format { get; }
        public string Pages { get; }

        public string SessionId { get; private set; } = null;

        private bool disposedValue;

        private IApiConnection conn = null;
        public ClientLow cl = null;

        private readonly ILogger _logger = Log.ForContext<ClientParse>();
        public ClientParse(IApiConnection connection,
            string pdfUrl, ClientLow.Commands command, HlidacStatu.DS.Api.TablesInDoc.Formats format = HlidacStatu.DS.Api.TablesInDoc.Formats.JSON, string pages = "all")
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
                        _logger.Debug($"try {i} Error 429 waiting because of {cl.ApiEndpoint}");
                        cl.Dispose();
                        cl = new ClientLow(conn.GetEndpointUrl(), conn.GetApiKey());
                        await Task.Delay(200 + 3000 * i);
                    }
                    else
                    {
                        this.SessionId = null;
                        _logger.Debug($"unexspected API response {cl.ApiEndpoint} {res.ErrorCode}:{res.ErrorDescription} ");

                        return res;
                    }

                } //for
                this.SessionId = null;
                _logger.Error($"no free resources");
                return new ApiResult<string>(false);

            }
            catch (Exception e)
            {
                return new ApiResult<string>(false) { ErrorCode = 500, ErrorDescription = e.ToString() };
            }
        }

        public async Task<ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>> GetSessionAsync()
        {
            if (string.IsNullOrEmpty(this.SessionId))
                return new ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>(false) { ErrorCode = 404, ErrorDescription = "sessionID is empty" };

            try
            {
                var res = await cl.GetSessionAsync(this.SessionId);
                res.Data.CamelotServer = cl.ApiEndpoint;
                return res;
            }
            catch (Exception e)
            {
                return new ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>(false) { ErrorDescription = e.ToString(), ErrorCode = 500 };
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
            catch (Exception)
            {
                return new ApiResult(false);
            }
        }
        public async Task<ApiResult<CamelotVersionData>> VersionAsync()
        {
            try
            {
                return await cl.VersionAsync();


            }
            catch (Exception)
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
            catch (Exception)
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
