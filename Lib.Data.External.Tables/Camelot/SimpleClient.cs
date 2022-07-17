using Devmasters.Log;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{

    public class SimpleClient
        : IDisposable
    {
        public string PdfUrl { get; }
        public ClientLow.Commands Command { get; }
        public CamelotResult.Formats Format { get; }
        public string Pages { get; }

        public string SessionId { get; private set; } = null;

        private bool disposedValue;

        private IApiConnection conn = null;
        public ClientLow cl = null;

        private static Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Camelot.Client.Simple",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                                .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Camelot.SimpleClient", "slog.txt",
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: Serilog.RollingInterval.Day,
                                    fileSizeLimitBytes: null,
                                    retainedFileCountLimit: 9,
                                    shared: true
                                    ));
        public SimpleClient(IApiConnection connection,
            string pdfUrl, ClientLow.Commands command, CamelotResult.Formats format = CamelotResult.Formats.JSON, string pages = "all")
        {
            PdfUrl = pdfUrl;
            Command = command;
            Format = format;
            Pages = pages;
            conn = connection;
            cl = new ClientLow(conn.GetEndpointUrl(), connection.GetApiKey());
        }

        public async Task<ApiResult<CamelotResult>> ParseFromUrl(int numberOfTries = 10)
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
                return new ApiResult<CamelotResult>(false);

            }
            catch (Exception e)
            {
                return new ApiResult<CamelotResult>(false) { ErrorCode = 500, ErrorDescription = e.ToString() };
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
