using Devmasters.Log;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public class Client
    {
        
        public static Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Camelot.Api",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                                .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Camelot.Api", "slog.txt",
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: Serilog.RollingInterval.Day,
                                    fileSizeLimitBytes: null,
                                    retainedFileCountLimit: 9,
                                    shared: true
                                    ));
        public static async Task<CamelotResult> GetTablesFromPDFAsync(
            string pdfUrl, ClientLow.Commands command, 
            CamelotResult.Formats format = CamelotResult.Formats.JSON, 
            string pages = "all", TimeSpan? executionTimeout = null,
            IApiConnection conn = null)
        {
            executionTimeout = executionTimeout ?? TimeSpan.FromMinutes(15);
            conn = conn ?? ConnectionPool.DefaultInstance();
            DateTime started = DateTime.Now;
            try
            {

                using (var cl = new ClientParse(conn, pdfUrl, command, format, pages))
                {
                    ApiResult<CamelotResult> res = null;
                    var session = await cl.StartSessionAsync();
                    if (session.Success == false)
                    {

                        return new CamelotResult()
                        {
                            Status = CamelotResult.Statuses.Error.ToString(),
                            ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                            FoundTables = 0,
                            ScriptOutput = session.ErrorDescription,
                            Format = format.ToString(),
                            CamelotServer = cl.cl.ApiEndpoint
                        };
                    }
                    do
                    {
                        System.Threading.Thread.Sleep(1000);
                        res = await cl.GetSessionAsync();
                        if (res.Success)
                        {
                            if (res.Data.Status.ToLower() == "done")
                            {
                                res.Data.ElapsedTimeInMs = (long)(DateTime.Now - started).TotalMilliseconds;
                                return res.Data;
                            }
                        }
                        else
                        {
                            return new CamelotResult()
                            {
                                Status = CamelotResult.Statuses.Error.ToString(),
                                ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                                FoundTables = 0,
                                ScriptOutput = "APIClient: Session disappeard",
                                Format = format.ToString(),
                                CamelotServer = cl.cl.ApiEndpoint
                            };
                        }
                        if ((DateTime.Now - started) > executionTimeout)
                        {
                            return new CamelotResult()
                            {
                                Status = CamelotResult.Statuses.Error.ToString(),
                                ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                                FoundTables = 0,
                                ScriptOutput = "APIClient: Timeout expired",
                                Format = format.ToString(),
                                CamelotServer = cl.cl.ApiEndpoint
                            };
                        }
                    } while (true);

                }
            }
            catch (Exception e)
            {
                return new CamelotResult()
                {
                    Status = CamelotResult.Statuses.Error.ToString(),
                    ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                    FoundTables = 0,
                    ScriptOutput = e.ToString(),
                    Format = format.ToString()
                };
            }

        }
    }
}
