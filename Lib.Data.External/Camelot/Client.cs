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
            var resLatt = await GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.lattice, format, pages);
            if (resLatt.Status != CamelotResult.Statuses.Error.ToString())
                res.Add(resLatt);
            var resStre = await GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.stream, format, pages);
            if (resStre.Status != CamelotResult.Statuses.Error.ToString())
                res.Add(resStre);

            return res.ToArray();
        }
        public static async Task<CamelotResult> GetTablesFromPDFAsync(string pdfUrl, ClientLow.Commands command, CamelotResult.Formats format = CamelotResult.Formats.HTML, string pages = "all", TimeSpan? executionTimeout = null)
        {
            executionTimeout = executionTimeout ?? TimeSpan.FromMinutes(2);

            DateTime started = DateTime.Now;
            using (var cl = new ClientLow(ConnectionPool.DefaultInstance(),pdfUrl, command, format, pages))
            {
                ApiResult<CamelotResult> res = null;
                var session = await cl.StartSessionAsync();
                do
                {
                    System.Threading.Thread.Sleep(500);
                    res = await cl.GetSessionAsync();
                    if (res.Success)
                    {
                        if (res.Data.Status.ToLower() == "done")
                            return res.Data;
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
    }
}
