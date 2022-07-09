using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Devmasters.Collections;

using HlidacStatu.Analysis.Page.Area;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.ES;

namespace HlidacStatu.Repositories
{
    public static class PageMetadataRepo
    {
        public static async Task<bool> ExistsAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return false;
            return await ExistsAsync(id);
        }
        public static async Task<bool> ExistsAsync(string id)
        {

            var es = await Manager.GetESClient_PageMetadataAsync();
            var res = await es.DocumentExistsAsync<PageMetadata>(id);
            return res.Exists;

        }

        public static async Task SaveAsync(PageMetadata item)
        {
            try
            {
                var client = await Repositories.ES.Manager.GetESClient_PageMetadataAsync();
                await client.IndexAsync<PageMetadata>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("PageMetadataRepo.Save error ", e);
                throw;
            }


        }

        public static async Task<bool> DeleteAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return false;
            return await DeleteAsync(id);
        }
        public static async Task<bool> DeleteAsync(string Id)
        {
            var client = await Manager.GetESClient_PageMetadataAsync();
            var res = await client.DeleteAsync<PageMetadata>(Id);
            return res.IsValid;
        }


        public static async Task<PageMetadata> LoadAsync(Smlouva smlouva, Smlouva.Priloha priloha, int stranka)
        {
            var id = PageMetadata.GetId(smlouva.Id, priloha?.UniqueHash(), stranka);
            if (string.IsNullOrEmpty(id))
                return null;
            return await LoadAsync(id);
        }

        public static async Task<PageMetadata> LoadAsync(string id)
        {
            var cl = await ES.Manager.GetESClient_PageMetadataAsync();

            var res = await cl.GetAsync<PageMetadata>(id);
            if (res.Found == false)
                return null;
            else if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
            else
            {
                return res.Source;
            }
        }




        public static void AnalyzePagesInBatch(
            Devmasters.Log.Logger logger, Action<string> logOutputFunc, Action<Devmasters.Batch.ActionProgressData> progressOutputFunc,
            string[] ids = null, string query = null, int? daysback = null, int? threads = null, bool force = false, bool debug = false,
            bool shuffle = false)
        {
            //default values
            threads = threads ?? 10;



            Console.WriteLine("Looking for records");
            if (debug)
                logger.Debug("zacernene: Looking for records");
            List<string> ids2Process = new List<string>();

            if (!string.IsNullOrEmpty(query))
            {
                if (debug)
                    logger.Debug("zacernene: setting searchFunc");

                ids2Process = Repositories.FilteredIdsRepo.CachedIds.Smlouvy(
                    new FilteredIdsRepo.QueryBatch()
                    {
                        Query = query,
                        LogOutputFunc = logOutputFunc,
                        ProgressOutputFunc = progressOutputFunc,
                        TaskPrefix = "AnalyzePagesInBatch getting ids "
                    }
                    )
                    .ToList();


            }
            else if (ids == null)
            {
                if (debug)
                    logger.Debug("zacernene: Getting all records");

                using (DbEntities db = new DbEntities())
                {
                    DateTime date = daysback == null ? DateTime.MinValue : DateTime.Now.Date.AddDays(-1 * daysback.Value);
                    ids2Process = db.SmlouvyIds.AsQueryable().Where(m => m.Active == 1 && m.Updated > date).Select(m => m.Id).ToList();
                }

            }
            else
            {
                if (debug)
                    logger.Debug("zacernene: getting IDS list");

                ids2Process = ids.ToList();
            }


            if (debug)
                logger.Debug($"zacernene: Found {ids2Process.Count()} smluv");




            DetectText.ModelFeeder mf = new DetectText.ModelFeeder(threads.Value + 5);
            if (shuffle)
                ids2Process = ids2Process.ShuffleMe().ToList();

            object lockObj = new object();
            Devmasters.Batch.ThreadManager.DoActionForAll(ids2Process,
                (s) =>
                {

                    Smlouva sml = SmlouvaRepo.LoadAsync(s, includePrilohy: false).Result;

                    if (debug)
                        logger.Debug($"zacernene: Smlouva {s} found {sml.Prilohy?.Count()} priloh");

                    foreach (var p in sml.Prilohy)
                    {
                        string tmpPdf = "";
                        try
                        {

                            tmpPdf = SmlouvaRepo.GetFileFromPrilohaRepository(p, sml);
                            if (HasPDFHeader(tmpPdf))
                            {
                                byte[] pdfBin = System.IO.File.ReadAllBytes(tmpPdf);
                                var numOfPages = 0;
                                using (var pdfStream = System.IO.File.OpenRead(tmpPdf))
                                {
                                    numOfPages = Analysis.Page.PdfTools.GetPageCount(pdfStream);
                                }
                                for (int ipage = 0; ipage < numOfPages; ipage++)
                                {
                                    int page = ipage + 1;

                                    if (PageMetadataRepo.ExistsAsync(sml, p, page).Result && force == false)
                                    {
                                        if (debug)
                                            logger.Debug($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} already done, skipping.");

                                        continue;
                                    }
                                    if (debug)
                                        logger.Debug($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} exporting JPEG");
                                    string fnJpg = "";
                                    try
                                    {
                                        fnJpg = tmpPdf + $"_p{page}.jpg";
                                        using (var pdfStream = System.IO.File.OpenRead(tmpPdf))
                                        {
                                            Analysis.Page.PdfTools.SaveJpeg(tmpPdf + $"_p{page}.jpg", pdfStream, page: ipage);
                                        }

                                        var model = mf.GetFreeModel();

                                        Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                                        sw.Start();

                                        if (debug)
                                            logger.Debug($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} detecting text");
                                        var dt = new Analysis.Page.Area.DetectText(model.LoadedModel, fnJpg);
                                        dt.AnalyzeImage();
                                        mf.ReturnModelBack(model);
                                        if (debug)
                                            logger.Debug($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} detecting zacerneni");
                                        var db = new Analysis.Page.Area.DetectBlack(fnJpg);
                                        db.AnalyzeImage();

                                        //dt in db resolution
                                        var dtC = dt.Result().GetForAnotherResolution(db.Result().ImageSize);

                                        PageMetadata pm = new PageMetadata();
                                        pm.SmlouvaId = sml.Id;
                                        pm.PrilohaId = p.UniqueHash();
                                        pm.PageNum = page;
                                        pm.Blurred = new PageMetadata.BlurredMetadata()
                                        {
                                            BlackenAreaBoundaries = db.Result().Boundaries
                                                .Select(m => (PageMetadata.BlurredMetadata.Boundary)m)
                                                .ToArray(),
                                            TextAreaBoundaries = dtC.Boundaries
                                                .Select(m => (PageMetadata.BlurredMetadata.Boundary)m)
                                                .ToArray(),
                                            AnalyzerVersion = "1.0",
                                            Created = DateTime.Now,
                                            ImageWidth = db.Result().ImageSize.Width,
                                            ImageHeight = db.Result().ImageSize.Height,
                                            BlackenArea = db.Result().GetTotalArea(),
                                            TextArea = dtC.GetTotalArea()
                                        };
                                        sw.Stop();

                                        logger.Info($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} detecting done in {sw.ElapsedMilliseconds}ms");

                                        if (debug)
                                            logger.Debug($"zacernene: Smlouva {s} priloha {p.nazevSouboru} page {page} saving");
                                        PageMetadataRepo.SaveAsync(pm)
                                            .ConfigureAwait(false).GetAwaiter().GetResult();

                                    }
                                    catch (Exception e)
                                    {
                                        logger.Error("zacernene ERROR: {smlouva} priloha {priloha} error.", e, sml.Id, p.UniqueHash());
                                        System.IO.File.Copy(fnJpg, fnJpg + ".error", true);
                                        System.IO.File.Copy(tmpPdf, tmpPdf + ".error", true);
                                    }
                                    finally
                                    {
                                        try
                                        {
                                            System.IO.File.Delete(fnJpg);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Error("zacernene ERROR: {smlouva} priloha {priloha} page {page}  cannot delete {file}.", ex, sml.Id, p.UniqueHash(), page, fnJpg);
                                            Devmasters.IO.IOTools.DeleteFile(fnJpg);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error("zacernene ERROR: {smlouva}  cannot get file.", e, sml.Id);
                            System.IO.File.Copy(tmpPdf, tmpPdf + ".error", true);
                        }
                        finally
                        {
                            try
                            {
                                System.IO.File.Delete(tmpPdf);
                            }
                            catch (Exception ex)
                            {
                                logger.Error("zacernene ERROR: {smlouva} priloha {priloha} cannot delete {file}.", ex, sml.Id, p.UniqueHash(), tmpPdf);
                                Devmasters.IO.IOTools.DeleteFile(tmpPdf);
                            }
                        }

                    }


                    return new Devmasters.Batch.ActionOutputData();

                }, true, maxDegreeOfParallelism: threads, logOutputFunc, progressOutputFunc, prefix: "AnalyzePagesInBatch ");


        }




        private static bool HasPDFHeader(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;
            if (!System.IO.File.Exists(filename))
                return false;

            byte[] b = new byte[4];
            using (var r = System.IO.File.OpenRead(filename))
            {
                r.Read(b, 0, 4);
            }
            byte[] pdfheader = new byte[] { 37, 80, 68, 70 };
            bool valid = true;
            for (int i = 0; i < 4; i++)
            {
                valid = valid && b[i] == pdfheader[i];
            }
            return valid;
        }

    }
}