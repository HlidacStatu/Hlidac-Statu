using Devmasters.Collections;
using HlidacStatu.Datasets;
using HlidacStatu.DS.Api;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Enhancers;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.Data.SqlClient;
using HlidacStatu.Extensions;
using ILogger = Serilog.ILogger;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [ApiExplorerSettings(IgnoreApi = false)]
    [SwaggerTag("OCR")]
    [ApiController()]
    [Route("api/v2/ocr")]
    public class ApiV2OCRController : ControllerBase
    {

        private ILogger _logger = Serilog.Log.ForContext<ApiV2OCRController>();

        static ItemToOcrQueue[] demodata = new ItemToOcrQueue[]
        {
            new ItemToOcrQueue()
            {
                ItemId="5247700",
                ItemType = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva.ToString(),
                Pk = 0,
            }
            ,new ItemToOcrQueue()
            {
                ItemId="f2b68b53f77c45b684a6314bac32e864",
                ItemType = HlidacStatu.DS.Api.OcrWork.DocTypes.VerejnaZakazka.ToString(),
                Pk = 0,
            }
            ,new ItemToOcrQueue()
            {
                ItemId="INS 3185/2020",
                ItemType = HlidacStatu.DS.Api.OcrWork.DocTypes.Insolvence.ToString(),
                Pk = 0,
            }
            ,new ItemToOcrQueue()
            {
                ItemId="KORNC53B3TJK",
                ItemSubType="veklep",
                ItemType = HlidacStatu.DS.Api.OcrWork.DocTypes.Dataset.ToString(),
                Pk = 0,
            }

        };

        static int demoCounter = 0;
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task>> GetTask(bool? demo = null,
            string itemId = null, string itemSubType = null, string itemType = null
            )
        {
            CheckRoleRecord(this.User.Identity.Name);
            ItemToOcrQueue item = null;
            int tries = 0;

        start:

            if (demo == true)
            {
                if (string.IsNullOrEmpty(itemId))
                {
                    var resd = demodata[demoCounter];
                    demoCounter++;
                    if (demoCounter >= demodata.Length)
                        demoCounter = 0;
                    item = resd;
                }
                else
                {
                    item = new ItemToOcrQueue()
                    {
                        ItemId = itemId,
                        ItemSubType = itemSubType,
                        ItemType = itemType,
                        Options = "{\"forceOCR\":true,\"ocrMissingOnly\":true,\"ocrLengthLessThan\":null,\"forceClassification\":false,\"forceTablesMining\":false,\"forceBlurredPages\":false}"
                    };
                }
            }
            else
            {
                var items = HlidacStatu.Entities.ItemToOcrQueue.TakeFromQueue(maxItems: 1);
                if (items == null)
                    return StatusCode(404);
                if (items.Count() == 0)
                    return StatusCode(404);
                item = items.First();
            }

            HlidacStatu.DS.Api.OcrWork.DocTypes _itemType = Enum.Parse<HlidacStatu.DS.Api.OcrWork.DocTypes>(item.ItemType);

            HlidacStatu.DS.Api.OcrWork.Task task = null;
            switch (_itemType)
            {
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva:
                    task = await GetSmlouva(item);
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.VerejnaZakazka:
                    task = await GetVZ(item);
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Dataset:
                    task = await GetDatasetItem(item);
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Insolvence:
                    task = await GetInsolvence(item);
                    break;
                default:
                    break;
            }
            if (task == null)
            {
                ItemToOcrQueue.SetDone(item.Pk, false, "record not found");
                if (tries < 20)
                {
                    tries++;
                    goto start;
                }
                else
                    return StatusCode(404);
            }
            if (task.docs == null || task.docs?.Length == 0)
            {
                ItemToOcrQueue.SetDone(item.Pk, true, "No docs to OCR");
                if (tries < 20)
                {
                    tries++;
                    goto start;
                }
                else
                    return StatusCode(404);
            }

            if (task.docs.Length > 50)
            {
                //split task into more
                ItemToOcrQueue secondItem = new ItemToOcrQueue()
                {
                    Created = item.Created,
                    ItemId = item.ItemId,
                    ItemSubType = item.ItemSubType,
                    ItemType = item.ItemType,
                    Options = item.Options,
                    Priority = item.Priority,
                    Started = null,
                    Done = null,
                    WaitForPK = item.Pk
                };
                await using (DbEntities db = new DbEntities())
                {
                    db.ItemToOcrQueue.Add(secondItem);
                    await db.SaveChangesAsync();
                }
                task.docs = task.docs.Take(50).ToArray();
            }

            return task;
        }
        private async Task<HlidacStatu.DS.Api.OcrWork.Task> GetDatasetItem(ItemToOcrQueue item)
        {
            //await DoOCRDatasetAsync(item.ItemSubType, null, new string[] { item.ItemId }, null, true, false, 100, lengthLess);
            if (string.IsNullOrWhiteSpace(item.ItemSubType.ToLower()))
            {
                return null;
            }
            if (! await DataSet.ExistsDatasetAsync(item.ItemSubType.ToLower()))
            {
                return null;
            }
            var ds = DataSet.GetCachedDataset(item.ItemSubType.ToLower());

            var dsitem = await ds.GetDataObjAsync(item.ItemId);
            List<Uri> uris = DataSet.GetFromItems_HsDocumentUrls(dsitem, false, 20);
            if (uris?.Count > 0)
            {
                HlidacStatu.DS.Api.OcrWork.Task res = new HlidacStatu.DS.Api.OcrWork.Task();
                res.taskId = item.Pk.ToString();
                res.type = HlidacStatu.DS.Api.OcrWork.DocTypes.Dataset;
                res.parentDocId = item.ItemId;
                res.docs = uris
                    .Select(m => new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                    {
                        origFilename = "",
                        prilohaId = m.AbsoluteUri,
                        url = m.AbsoluteUri
                    })
                    .ToArray();

                return res;
            }
            else
                return null;
        }

        private async Task<HlidacStatu.DS.Api.OcrWork.Task> GetInsolvence(ItemToOcrQueue item)
        {
            HlidacStatu.Entities.Insolvence.Rizeni? insolv = (await InsolvenceRepo.LoadFromEsAsync(item.ItemId, false, false))?.Rizeni;

            if (insolv == null)
                return null;
            HlidacStatu.DS.Api.OcrWork.Task res = new HlidacStatu.DS.Api.OcrWork.Task();
            res.taskId = item.Pk.ToString();
            res.type = HlidacStatu.DS.Api.OcrWork.DocTypes.Insolvence;
            res.parentDocId = item.ItemId;
            res.docs = insolv.Dokumenty
                //.Where(m => Uri.TryCreate(m.GetDocumentUrlToDownload(), UriKind.Absolute, out _))
                .Where(m => item.GetOptions().forceOCR || HlidacStatu.Util.ParseTools.EnoughExtractedTextCheck(m.WordCount, m.Lenght, m.WordCount, 0) == false)
                .Select(m => new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                {
                    origFilename = "",
                    prilohaId = m.Id,
                    url = m.Url
                })
                .ToArray();

            return res;
        }

        private async Task<HlidacStatu.DS.Api.OcrWork.Task> GetVZ(ItemToOcrQueue item)
        {
            var vz = await VerejnaZakazkaRepo.LoadFromESAsync(item.ItemId);

            if (vz == null)
                return null;
            HlidacStatu.DS.Api.OcrWork.Task res = new HlidacStatu.DS.Api.OcrWork.Task();
            res.taskId = item.Pk.ToString();
            res.type = HlidacStatu.DS.Api.OcrWork.DocTypes.VerejnaZakazka;
            res.parentDocId = item.ItemId;
            res.docs = vz.Dokumenty
                .Where(m => item.GetOptions().forceOCR || HlidacStatu.Util.ParseTools.EnoughExtractedTextCheck(m.WordCount, m.Lenght, m.WordCount, 0) == false)
                .Where(m => Uri.TryCreate(m.GetHlidacUrl(item.ItemId), UriKind.Absolute, out _))
                .Select(m => new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                {
                    origFilename = m.Name,
                    prilohaId = m.GetUniqueId(),
                    url = m.GetHlidacUrl(item.ItemId)
                })
                .ToArray();

            return res;
        }

        private async Task<HlidacStatu.DS.Api.OcrWork.Task> GetSmlouva(ItemToOcrQueue item)
        {
            var sml = await SmlouvaRepo.LoadAsync(item.ItemId, includePlaintext: false);

            if (sml == null)
                return null;
            HlidacStatu.DS.Api.OcrWork.Task res = new HlidacStatu.DS.Api.OcrWork.Task();
            res.taskId = item.Pk.ToString();
            res.type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva;
            res.parentDocId = item.ItemId;
            res.docs = sml.Prilohy
                .Where(m => item.GetOptions().forceOCR || m.EnoughExtractedText == false)
                .Select(m => new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                {
                    origFilename = m.nazevSouboru,
                    prilohaId = m.UniqueHash(),
                    url = m.odkaz
                })
                .ToArray();

            return res;
        }

  

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.OcrWork.Task res)
        {
            CheckRoleRecord(this.User.Identity.Name);
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            try
            {
                //hangfire do not have issues (according to AI with async jobs) ok then...
                _ = Hangfire.BackgroundJob.Enqueue(() => DoSaveAsync(res));
                bool done = false;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
                return StatusCode(500);
            }
            finally
            {
                sw.Stop();
                if (sw.ElapsedMilliseconds > 10000)
                {
                    _logger.Warning("Too log task {taskId} document {docId} of type {docType} elapsed {elapsed_ms}", res.taskId, res.parentDocId, res.type, sw.ElapsedMilliseconds);
                }
            }
            return StatusCode(200);
        }

        //musi byt public kvuli                  _ = Hangfire.BackgroundJob.Enqueue(() => DoSave(res));

        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task DoSaveAsync(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            try
            {
                switch (res.type)
                {
                    case HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva:
                        _ = await SaveSmlouva(res);
                        ItemToOcrQueue.SetDone(int.Parse(res.taskId), true);
                        break;
                    case HlidacStatu.DS.Api.OcrWork.DocTypes.VerejnaZakazka:
                        _ = await SaveVZ(res);
                        ItemToOcrQueue.SetDone(int.Parse(res.taskId), true);
                        break;
                    case HlidacStatu.DS.Api.OcrWork.DocTypes.Dataset:
                        _ = await SaveDataset(res);
                        ItemToOcrQueue.SetDone(int.Parse(res.taskId), true);
                        break;
                    case HlidacStatu.DS.Api.OcrWork.DocTypes.Insolvence:
                        _ = await SaveInsolvence(res);
                        ItemToOcrQueue.SetDone(int.Parse(res.taskId), true);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
                throw;
            }

        }
        private async Task<bool> SaveSmlouva(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            List<Smlouva.Priloha> newPrilohy = new List<Smlouva.Priloha>();
            bool changed = false;

            var sml = await SmlouvaRepo.LoadAsync(res.parentDocId);
            if (sml == null)
            {
                _logger.Error("Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
                return false;
            }
            foreach (var doc in res.docs)
            {
                var attIdx = sml.Prilohy
                        .Select((pr, i) => new { i, pr })
                        .Where(m => m.pr.UniqueHash() == doc.prilohaId)
                        .FirstOrDefault();
                int i = 0;
                Smlouva.Priloha att = null;
                if (attIdx != null)
                {
                    i = attIdx.i;
                    att = attIdx.pr;
                }
                else
                {
                    _logger.Warning("Cannot find priloha {prilohaId} for smlouva {docId}", doc.prilohaId, res.parentDocId);
                    i = 0;
                    att = new Smlouva.Priloha();
                    newPrilohy.Add(att);

                }
                if (doc.result?.Documents?.Count > 1)
                {
                    //first
                    att.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(doc.result.Documents[0].Text);
                    if (doc.result.Documents[0].UsedOCR)
                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                    else
                        att.PlainTextContentQuality = DataQualityEnum.Parsed;

                    att.ContentType = doc.result.Documents[0].ContentType;
                    if (doc.result.Documents[0].FileMetadata?.Count() > 0)
                        att.FileMetadata = doc.result.Documents[0].FileMetadata
                            .Select(m => new Smlouva.Priloha.KeyVal() { Key = m.Key, Value = m.Value }).ToArray();

                    await att.UpdateStatisticsAsync(sml);

                    att.LastUpdate = DateTime.Now;

                    if (att.EnoughExtractedText)
                    {
                        if (att.PlainTextContentQuality == DataQualityEnum.Estimated)
                            sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));
                        else
                            sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));
                    }

                    for (int ii = 1; ii < doc.result.Documents.Count; ii++)
                    {

                        var att1 = new Smlouva.Priloha();
                        att1.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(doc.result.Documents[ii].Text);
                        if (doc.result.Documents[ii].UsedOCR)
                            att1.PlainTextContentQuality = DataQualityEnum.Estimated;
                        else
                            att1.PlainTextContentQuality = DataQualityEnum.Parsed;

                        att1.ContentType = doc.result.Documents[ii].ContentType;
                        att1.odkaz = att.odkaz + "#num" + ii;

                        if (doc.result.Documents[ii].FileMetadata?.Count() > 0)
                            att1.FileMetadata = doc.result.Documents[ii].FileMetadata
                                .Select(m => new Smlouva.Priloha.KeyVal() { Key = m.Key, Value = m.Value }).ToArray();

                        await att1.UpdateStatisticsAsync(sml);

                        att1.LastUpdate = DateTime.Now;

                        if (att1.EnoughExtractedText)
                        {
                            if (att1.PlainTextContentQuality == DataQualityEnum.Estimated)
                                sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + (sml.Prilohy.Count() + ii).ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));
                            else
                                sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + (sml.Prilohy.Count() + ii).ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));

                        }
                        newPrilohy.Add(att1);
                        changed = true;
                    }
                    //others

                } //docs.count > 1
                else if (doc.result?.Documents?.Count == 1)
                {
                    att.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(doc.result.Documents[0].Text);
                    if (doc.result.Documents[0].UsedOCR)
                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                    else
                        att.PlainTextContentQuality = DataQualityEnum.Parsed;
                    att.ContentType = doc.result.Documents[0].ContentType;

                    if (doc.result.Documents[0].FileMetadata?.Count() > 0)
                        att.FileMetadata = doc.result.Documents[0].FileMetadata
                            .Select(m => new Smlouva.Priloha.KeyVal() { Key = m.Key, Value = m.Value }).ToArray();

                    await att.UpdateStatisticsAsync(sml);

                    att.LastUpdate = DateTime.Now;

                    if (att.EnoughExtractedText)
                    {
                        if (att.PlainTextContentQuality == DataQualityEnum.Estimated)
                            sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z OCR dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));
                        else
                            sml.Enhancements = sml.Enhancements.AddOrUpdate(new Enhancement("Text přílohy extrahován z obsahu dokumentu ", "", "item.Prilohy[" + i.ToString() + "].PlainTextContent", "", "", "FullOcrMinion"));

                    }
                    changed = true;
                }//docs.count = 1
                if (newPrilohy.Count > 0)
                    sml.Prilohy = sml.Prilohy.Concat(newPrilohy).ToArray();
            }
            if (changed)
            {
                _ = await SmlouvaRepo.SaveAsync(sml, fireOCRDone: true);
            }
            return true;
        }
        private async Task<bool> SaveInsolvence(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            HlidacStatu.Entities.Insolvence.Rizeni? insolv = (await InsolvenceRepo.LoadFromEsAsync(res.parentDocId, true, false))?.Rizeni;
            if (insolv == null)
            {
                _logger.Error("Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
                return false;
            }
            foreach (var doc in res.docs)
            {
                var att = insolv.Dokumenty
                    .FirstOrDefault(m => m.Id == doc.prilohaId);
                if (att != null && doc.result != null)
                {
                    var mergedDoc = doc.result.MergedDocuments();
                    att.PlainText = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(mergedDoc.Text);
                    att.Lenght = att.PlainText.Length;
                    att.WordCount = Devmasters.TextUtil.CountWords(att.PlainText);

                    att.LastUpdate = DateTime.Now;
                }//docs.count = 1
            }
            await InsolvenceRepo.SaveRizeniAsync(insolv);
            return true;
        }
        private async Task<bool> SaveVZ(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            var vz = await VerejnaZakazkaRepo.LoadFromESAsync(res.parentDocId);
            if (vz == null)
            {
                _logger.Error("Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
                return false;
            }
            foreach (var doc in res.docs)
            {
                HlidacStatu.Entities.VZ.VerejnaZakazka.Document? att = vz.Dokumenty
                    .FirstOrDefault(m => m.GetUniqueId() == doc.prilohaId);
                if (att != null && doc.result != null)
                {
                    var mergedDoc = doc.result.MergedDocuments();
                    att.ContentType = mergedDoc.ContentType;
                    att.PlainText = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(mergedDoc.Text);
                    if (mergedDoc.UsedOCR)
                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                    else
                        att.PlainTextContentQuality = DataQualityEnum.Parsed;
                    att.ContentType = mergedDoc.ContentType;
                    att.Lenght = att.PlainText.Length;
                    att.WordCount = Devmasters.TextUtil.CountWords(att.PlainText);
                    att.LastUpdate = DateTime.Now;
                }//docs.count = 1
            }
            await VerejnaZakazkaRepo.UpsertAsync(vz, sendToOcr: false, updatePosledniZmena: false, shouldDownloadFile: false);
            return true;
        }
        private async Task<bool> SaveDataset(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            var task = ItemToOcrQueue.GetTask(Convert.ToInt32(res.taskId));
            if (task == null)
            {
                _logger.Error("Cannot get task {taskId}.", res.taskId);
                return false;
            }

            var ds = DataSet.GetCachedDataset(task.ItemSubType.ToLower());
            if (ds == null)
            {
                _logger.Error("Cannot get dataset {datasetname} from task {taskId}.", task.ItemSubType.ToLower(), res.taskId);
                return false;
            }
            var item = await ds.GetDataObjAsync(task.ItemId);
            if (item == null)
            {
                _logger.Error("Cannot get item {datasetitemid} dataset {datasetname} from task {taskId}.", task.ItemId, task.ItemSubType.ToLower(), res.taskId);
                return false;
            }

            var jobj = (Newtonsoft.Json.Linq.JObject)item;
            var jpaths = jobj
                .SelectTokens("$..HsProcessType")
                .ToArray();
            var jpathObjs = jpaths.Select(j => j.Parent.Parent).ToArray();

            foreach (var jo in jpathObjs)
            {
                if (DataSet.OCRCommands.Contains(jo["HsProcessType"].Value<string>()))
                {
                    if (jo["DocumentUrl"] != null &&
                          Uri.TryCreate(jo["DocumentUrl"].Value<string>(), UriKind.Absolute, out var uri2Ocr))
                    {
                        if (res.docs.Any(m => m.url.ToLower() == uri2Ocr.AbsoluteUri.ToLower()))
                        {
                            var resDocs = res.docs
                                    .Where(m => m.url.ToLower() == uri2Ocr.AbsoluteUri.ToLower());
                            foreach (var doc in resDocs)
                            {
                                if (doc.result != null)
                                    jo["DocumentPlainText"] = HlidacStatu.DS.Api.OcrWork.Result.MergeDocumentsIntoOne(
                                            doc.result.Documents
                                        ).Text;

                            }
                        }
                    }
                }
            }

            await ds.AddDataAsync(jobj.ToString(), task.ItemId, jobj["DbCreatedBy"].Value<string>(), true, true);
            return true;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {
            CheckRoleRecord(this.User.Identity.Name);

            _logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "TablesInDocsMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private void CheckRoleRecord(string username)
        {
            return;
            //check if user is in blurredAPIAccess roles
            try
            {
                var found = HlidacStatu.Connectors.DirectDB.Instance.GetList<string, string>(
                    "select u.Id, ur.UserId from AspNetUsers u left join AspNetUserRoles ur on u.id = ur.UserId and ur.RoleId='e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' where u.UserName = @username",
                    System.Data.CommandType.Text, new SqlParameter[] { new SqlParameter("username", username) }
                    );
                if (found.Count() == 0)
                    return;
                if (found.Count() == 1 && found.First().Item2 == null)
                {
                    HlidacStatu.Connectors.DirectDB.Instance.NoResult(
                        @"insert into AspNetUserRoles select  (select id from AspNetUsers where Email like @username) as userId,'e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' as roleId",
                        System.Data.CommandType.Text, new SqlParameter[] { new SqlParameter("username", username) }
                        );
                }

            }
            catch (Exception e)
            {
                _logger.Error(e, "cannot add {username} to the role blurredAPIAccess", username);
            }

        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(OcrWork.DocTypes itemType, string itemId, string itemSubType = null,
            OcrWork.TaskPriority priority = OcrWork.TaskPriority.Standard,
            OcrWork.TaskOptions options = null)
        {
            ItemToOcrQueue.AddNewTask(itemType,itemId, itemSubType, priority, options);
            return true;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Stats")]
        public async Task<ActionResult<OCRStatistics>> Stats()
        {
            long OCRQueueWaiting = await HlidacStatu.Connectors.DirectDB.Instance.GetValueAsync<int>("select count(*) from ItemToOcrQueue with (nolock) where started is null");
            long OCRQueueRunning = await HlidacStatu.Connectors.DirectDB.Instance.GetValueAsync<int>("select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null"); ;
            long OCRProcessing = await HlidacStatu.Connectors.DirectDB.Instance.GetValueAsync<int>("select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is not null");


            DateTime now = DateTime.Now;
            var res = new OCRStatistics()
            {
                waiting = OCRQueueWaiting,
                processing = OCRQueueRunning,
                done = OCRProcessing,
                total = OCRProcessing + OCRQueueRunning + OCRQueueWaiting
            };

            return res;
        }



    }
}