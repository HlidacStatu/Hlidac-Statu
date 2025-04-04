﻿using HlidacStatu.Lib.Data.External.Tables.Camelot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class PDF
    {
        private static ILogger _logger = Log.ForContext(typeof(PDF));
        public static async Task<HlidacStatu.DS.Api.TablesInDoc.Result[]> GetMaxTablesFromPDFAsync(
            string pdfUrl, HlidacStatu.DS.Api.TablesInDoc.Formats format = HlidacStatu.DS.Api.TablesInDoc.Formats.JSON, string pages = "all", 
            TimeSpan? executionTimeout = null,
            IApiConnection conn = null
            )
        {
            List<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult> res = new List<HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult>();
            HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult resLatt = null;
            HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult resStre = null;
            ParallelOptions po = new ParallelOptions();

            //if (System.Diagnostics.Debugger.IsAttached)
            //    po = new ParallelOptions() { MaxDegreeOfParallelism = 1 };

            Parallel.Invoke(po,
                 () =>
                 {
                     resLatt = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.lattice, format, pages,executionTimeout,conn).Result;
                     if (resLatt.Status != HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString())
                         res.Add(resLatt);
                     _logger.Debug($"PDF {pdfUrl} done Latt in {resLatt.ElapsedTimeInMs}ms on {resLatt.CamelotServer}, status {resLatt.Status}");
                 },
                () =>
                {
                    resStre = Client.GetTablesFromPDFAsync(pdfUrl, ClientLow.Commands.stream, format, pages, executionTimeout, conn).Result;
                    if (resStre.Status != HlidacStatu.DS.Api.TablesInDoc.ApiOldCamelotResult.Statuses.Error.ToString())
                        res.Add(resStre);
                    _logger.Debug($"PDF {pdfUrl} done Stream in {resStre?.ElapsedTimeInMs}ms on {resStre.CamelotServer}, status {resStre.Status}");
                });
            if (resLatt.ErrorOccured() && resStre.ErrorOccured())
                return null;

            return res
                .Select(m => (HlidacStatu.DS.Api.TablesInDoc.Result)m)
                .ToArray()
                ;
        }
    }
}
