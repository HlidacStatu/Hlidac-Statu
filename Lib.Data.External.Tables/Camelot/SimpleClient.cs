using HlidacStatu.DS.Api;
using System;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{

    public class SimpleClient
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

        private static Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Camelot.Client.Simple");
        public SimpleClient(IApiConnection connection,
            string pdfUrl, ClientLow.Commands command, HlidacStatu.DS.Api.TablesInDoc.Formats format = HlidacStatu.DS.Api.TablesInDoc.Formats.JSON, string pages = "all")
        {
            PdfUrl = pdfUrl;
            Command = command;
            Format = format;
            Pages = pages;
            conn = connection;
            cl = new ClientLow(conn.GetEndpointUrl(), connection.GetApiKey());
        }

        public async Task<ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>> ParseFromUrl(int numberOfTries = 10)
        {
            try
            {
                for (int i = 0; i < numberOfTries; i++)
                {
                    var res = await cl.ParseFromUrl(this.PdfUrl, this.Command, this.Format, this.Pages);
                    if (res.ErrorCode == 0)
                    {
                        return res;
                    }
                    else if (res.ErrorCode == 429)
                    {
                        logger.Debug($"try {i} Error 429 waiting because of {cl.ApiEndpoint}");
                        cl.Dispose();
                        cl = new ClientLow(conn.GetEndpointUrl(), conn.GetApiKey());
                        System.Threading.Thread.Sleep(200 + 3000 * i);
                    }
                    else
                    {
                        logger.Debug($"unexspected API response {cl.ApiEndpoint} {res.ErrorCode}:{res.ErrorDescription} ");

                        return res;
                    }

                } //for
                this.SessionId = null;
                logger.Error($"no free resources");
                return new ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>(false);

            }
            catch (Exception e)
            {
                return new ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>(false) { ErrorCode = 500, ErrorDescription = e.ToString() };
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
            if (cl != null)
            {
                await cl.DisposeAsync();
                cl = null;
            }
            Dispose(false);
        }
    }
}
