using HlidacStatu.Lib.Data.External.Tables.Camelot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class PDF
    {
        public static async Task<Result[]> GetMaxTablesFromPDFAsync(string pdfUrl, CamelotResult.Formats format = CamelotResult.Formats.JSON, string pages = "all", TimeSpan? executionTimeout = null)
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
                     resLatt = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.lattice, format, pages).Result;
                     if (resLatt.Status != CamelotResult.Statuses.Error.ToString())
                         res.Add(resLatt);
                     Client.Logger.Debug($"PDF {pdfUrl} done in {sw.ElapsedMilliseconds}ms on {resLatt.CamelotServer}");
)
                 },
                () =>
                {
                    resStre = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.stream, format, pages).Result;
                    if (resStre.Status != CamelotResult.Statuses.Error.ToString())
                        res.Add(resStre);
                });
            if (resLatt.ErrorOccured() && resStre.ErrorOccured())
                return null;

            return res
                //.Select(m => new Result()
                //{
                //    Algorithm = m.Algorithm,
                //    ElapsedTimeInMs = m.ElapsedTimeInMs,
                //    Format = m.Format,
                //    FoundTables = m.FoundTables,
                //    Status = m.Status,
                //    Tables = m.Tables
                //})
                .Select(m => (Result)m)
                .ToArray()
                ;
        }
    }
}
