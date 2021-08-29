using NPOI.XWPF.UserModel;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Tables
{
    public static class Word
    {
        private static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("HlidacStatu.Lib.Data.External.Tables.Word");

        public static Result[] GetTablesFromWord(Uri url, string filename)
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
                    tmpFn = System.IO.Path.GetTempFileName() + ext;
                    wc.DownloadFile(url, tmpFn);
                    return GetTablesFromWord(tmpFn);
                }
                catch (Exception e)
                {
                    logger.Error($"GetTablesFromWord {url.ToString()}", e);
                    throw e;
                }
                finally
                {
                    if (!string.IsNullOrEmpty(tmpFn))
                    {
                        System.IO.File.Delete(tmpFn);
                        System.IO.File.Delete(tmpFn + ext);
                    }
                }
            }
        }
        public static Result[] GetTablesFromWord(string fn)
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
        private static Result[] GetTablesFromWordDOC(string fn)
        {
            // not implemented
            logger.Warning($"DOC is not supported {fn}");
            return null;

        }
        private static Result[] GetTablesFromWordDOCX(string fn)
        {
            Result res = new Result();
            var tbls = new List<Result.Table>();

            if (File.Exists(fn) == false)
                return null;

            Devmasters.DT.StopWatchLaps stopWatchLaps = new Devmasters.DT.StopWatchLaps();
            var swint = stopWatchLaps.AddAndStartLap("NPOI");
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

                        //simulate Camelot JSON result
                        var jsonCols = new Newtonsoft.Json.Linq.JObject();
                        for (int ci = 0; ci < r.GetTableCells().Count; ci++)
                        {
                            var c = r.GetTableCells()[ci];
                            var cont = c.GetTextRecursively();
                            cont = Devmasters.TextUtil.NormalizeToBlockText(cont)?.Trim();
                            //simulate Camelot JSON result
                            jsonCols.Add(ci.ToString(), cont);
                        }
                        jsonTbl.Add(jsonCols);
                        tblCount++;
                    }
                    tbls.Add(new Result.Table()
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

            return new Result[] { res };
        }

    }


}
