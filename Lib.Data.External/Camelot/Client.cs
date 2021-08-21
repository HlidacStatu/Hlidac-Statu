using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    public class Client
    {

        public static async Task<CamelotResult[]> GetMaxTablesFromPDFAsync(string pdfUrl, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all", TimeSpan? executionTimeout = null)
        {
            List<CamelotResult> res = new List<CamelotResult>();
            CamelotResult resLatt = null;
            CamelotResult resStre = null;
            ParallelOptions po = new ParallelOptions();
            if (System.Diagnostics.Debugger.IsAttached)
                po = new ParallelOptions() { MaxDegreeOfParallelism = 1 };

            Parallel.Invoke(po,
                 () =>
                {
                    resLatt = GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.lattice, format, pages).Result;
                    if (resLatt.Status != CamelotResult.Statuses.Error.ToString())
                        res.Add(resLatt);
                },
                () =>
                {
                    resStre = GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.stream, format, pages).Result;
                    if (resStre.Status != CamelotResult.Statuses.Error.ToString())
                        res.Add(resStre);
                });
            if (resLatt.ErrorOccured() && resStre.ErrorOccured())
                return null;

            return res.ToArray();
        }
        public static async Task<CamelotResult> GetTablesFromPDFAsync(string pdfUrl, ClientLow.Commands command, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all", TimeSpan? executionTimeout = null)
        {
            executionTimeout = executionTimeout ?? TimeSpan.FromMinutes(15);

            DateTime started = DateTime.Now;
            try
            {

                using (var cl = new ClientParse(ConnectionPool.DefaultInstance(), pdfUrl, command, format, pages))
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
                            Format = format.ToString()
                        };
                    }
                    do
                    {
                        System.Threading.Thread.Sleep(1000);
                        res = await cl.GetSessionAsync();
                        if (res.Success)
                        {
                            if (res.Data.Status.ToLower() == "done")
                                return res.Data;
                        }
                        else
                        {
                            return new CamelotResult()
                            {
                                Status = CamelotResult.Statuses.Error.ToString(),
                                ElapsedTimeInMs = (long)executionTimeout.Value.TotalMilliseconds,
                                FoundTables = 0,
                                ScriptOutput = "APIClient: Session disappeard",
                                Format = format.ToString()
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
                                Format = format.ToString()
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
