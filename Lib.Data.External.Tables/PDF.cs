using HlidacStatu.Lib.Data.External.Tables.Camelot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class PDF
    {
        public static async Task<HlidacStatu.DS.Api.TablesInDoc.Result[]> GetMaxTablesFromPDFAsync(
            string pdfUrl, HlidacStatu.DS.Api.TablesInDoc.ApiResult.Formats format = HlidacStatu.DS.Api.TablesInDoc.ApiResult.Formats.JSON, string pages = "all", 
            TimeSpan? executionTimeout = null,
            IApiConnection conn = null
            )
        {
            List<HlidacStatu.DS.Api.TablesInDoc.ApiResult> res = new List<HlidacStatu.DS.Api.TablesInDoc.ApiResult>();
            HlidacStatu.DS.Api.TablesInDoc.ApiResult resLatt = null;
            HlidacStatu.DS.Api.TablesInDoc.ApiResult resStre = null;
            ParallelOptions po = new ParallelOptions();

            //if (System.Diagnostics.Debugger.IsAttached)
            //    po = new ParallelOptions() { MaxDegreeOfParallelism = 1 };

            Parallel.Invoke(po,
                 () =>
                 {
                     resLatt = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.lattice, format, pages,executionTimeout,conn).Result;
                     if (resLatt.Status != HlidacStatu.DS.Api.TablesInDoc.ApiResult.Statuses.Error.ToString())
                         res.Add(resLatt);
                     Client.logger.Debug($"PDF {pdfUrl} done Latt in {resLatt.ElapsedTimeInMs}ms on {resLatt.CamelotServer}, status {resLatt.Status}");
                 },
                () =>
                {
                    resStre = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.stream, format, pages, executionTimeout, conn).Result;
                    if (resStre.Status != HlidacStatu.DS.Api.TablesInDoc.ApiResult.Statuses.Error.ToString())
                        res.Add(resStre);
                    Client.logger.Debug($"PDF {pdfUrl} done Stream in {resStre?.ElapsedTimeInMs}ms on {resStre.CamelotServer}, status {resStre.Status}");
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
                .Select(m => (HlidacStatu.DS.Api.TablesInDoc.Result)m)
                .ToArray()
                ;
        }
    }
}
