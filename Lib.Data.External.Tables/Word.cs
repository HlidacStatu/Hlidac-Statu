using Devmasters.Log;

using NPOI.XWPF.UserModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class Word
    {
        private static Devmasters.Log.Logger logger = Devmasters.Log.Logger.CreateLogger("HlidacStatu.Lib.Data.External.Tables.Word",
                            Devmasters.Log.Logger.DefaultConfiguration()
                                .Enrich.WithProperty("codeversion", System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString())
                                .AddFileLoggerFilePerLevel("c:/Data/Logs/HlidacStatu/Tbls.Word", "slog.txt",
                                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {SourceContext} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                                    rollingInterval: Serilog.RollingInterval.Day,
                                    fileSizeLimitBytes: null,
                                    retainedFileCountLimit: 9,
                                    shared: true
                                    ));

        public static HlidacStatu.Entities.DocTables.Result[] GetTablesFromWord(Uri url, string filename)
        {
            if (filename?.EndsWith(".doc") == true)
            {
                logger.Warning($"DOC is not supported {url.ToString()}");
                return null;
            }

            using (System.Net.WebClient wc = new System.Net.WebClient())
            {
                string tmpFn = "";
                string ext = "." + filename;
                try
                {
                    tmpFn = Path.GetTempFileName() + ext;
                    wc.DownloadFile(url, tmpFn);
                    return GetTablesFromWord(tmpFn);
                }
                catch (Exception e)
                {
                    logger.Error($"GetTablesFromWord {url.ToString()}", e);
                    throw;
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tmpFn))
                    {
                        File.Delete(tmpFn);
                        File.Delete(tmpFn + ext);
                    }
                }
            }
        }
        public static HlidacStatu.Entities.DocTables.Result[] GetTablesFromWord(string fn)
        {
            try
            {

                if (File.Exists(fn) == false)
                    return null;
                if (fn.EndsWith(".docx"))
                    return GetTablesFromWordDOCX(fn);
                if (fn.EndsWith(".doc"))
                    return GetTablesFromWordDOC(fn);
            }
            catch (Exception e)
            {
                logger.Error($"GetTablesFromWord {fn}", e);
            }
            return null;
        }
        private static HlidacStatu.Entities.DocTables.Result[] GetTablesFromWordDOC(string fn)
        {
            // not implemented
            logger.Warning($"DOC is not supported {fn}");
            return null;

        }
        private static HlidacStatu.Entities.DocTables.Result[] GetTablesFromWordDOCX(string fn)
        {
            HlidacStatu.Entities.DocTables.Result res = new HlidacStatu.Entities.DocTables.Result();
            var tbls = new List<HlidacStatu.Entities.DocTables.Result.Table>();

            if (File.Exists(fn) == false)
                return null;

            Devmasters.DT.StopWatchLaps stopWatchLaps = new Devmasters.DT.StopWatchLaps();
            var swint = stopWatchLaps.StopPreviousAndStartNextLap("NPOI");
            int tblCount = 1;
            using (var rs = File.OpenRead(fn))
            {
                var doc = new XWPFDocument(rs);
                foreach (var xtbl in doc.Tables)
                {
                    int cols = xtbl.Rows.Select(r => r.GetTableCells().Count).Max();
                    var jsonTbl = new Newtonsoft.Json.Linq.JArray();
                    for (int ri = 0; ri < xtbl.Rows.Count; ri++)
                    {
                        var r = xtbl.Rows[ri];

                        //simulate Camelot JSON HlidacStatu.Entities.DocTables.Result
                        var jsonCols = new Newtonsoft.Json.Linq.JObject();
                        for (int ci = 0; ci < r.GetTableCells().Count; ci++)
                        {
                            var c = r.GetTableCells()[ci];
                            var cont = c.GetTextRecursively();
                            cont = Devmasters.TextUtil.NormalizeToBlockText(cont)?.Trim();
                            //simulate Camelot JSON HlidacStatu.Entities.DocTables.Result
                            jsonCols.Add(ci.ToString(), cont);
                        }
                        jsonTbl.Add(jsonCols);
                        tblCount++;
                    }
                    tbls.Add(new HlidacStatu.Entities.DocTables.Result.Table()
                    {
                        Content = jsonTbl.ToString(),
                        Page = 0,
                        TableInPage = tblCount
                    });
                }
            }
            swint.Stop();

            res.Tables = tbls.ToArray();
            res.FoundTables = res.Tables.Length;
            res.Status = "Done";
            res.Format = "NPOI.Docx";
            res.ElapsedTimeInMs = (int)swint.ExactElapsedMs;

            return new HlidacStatu.Entities.DocTables.Result[] { res };
        }

    }


}
