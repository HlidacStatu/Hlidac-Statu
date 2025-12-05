using HlidacStatu.DS.Api;
using System;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    public class Client
    {
        public static async Task<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult> GetTablesFromPDFAsync(
            string pdfUrl, ClientLow.Commands command, 
            HlidacStatu.DS.Api.TablesInDoc.Formats format = HlidacStatu.DS.Api.TablesInDoc.Formats.JSON, 
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
                    ApiResult<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult> res = null;
                    var session = await cl.StartSessionAsync();
                    if (session.Success == false)
                    {

                        return new HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult()
                        {
                            Status = HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString(),
                            ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                            FoundTables = 0,
                            ScriptOutput = session.ErrorDescription,
                            Format = format.ToString(),
                            CamelotServer = cl.cl.ApiEndpoint
                        };
                    }
                    do
                    {
                        await Task.Delay(1000);
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
                            return new HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult()
                            {
                                Status = HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString(),
                                ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                                FoundTables = 0,
                                ScriptOutput = "APIClient: Session disappeard",
                                Format = format.ToString(),
                                CamelotServer = cl.cl.ApiEndpoint
                            };
                        }
                        if ((DateTime.Now - started) > executionTimeout)
                        {
                            return new HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult()
                            {
                                Status = HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString(),
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
                return new HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult()
                {
                    Status = HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString(),
                    ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                    FoundTables = 0,
                    ScriptOutput = e.ToString(),
                    Format = format.ToString()
                };
            }

        }
    }
}
