using Devmasters.Collections;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Enhancers;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Data.SqlClient;
using static HlidacStatu.DS.Api.BlurredPage;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [ApiExplorerSettings(IgnoreApi = false)]
    [SwaggerTag("OCR")]
    [ApiController()]
    [Route("api/v2/ocr")]
    public class ApiV2OCRController : ControllerBase
    {
        static object lockObj = new object();
        static ApiV2OCRController()
        {
        }



        static ItemToOcrQueue[] demodata = new ItemToOcrQueue[]
        {
            new ItemToOcrQueue()
            {
                ItemId="5247700",
                ItemType = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva.ToString(),
                Pk = 0,
            }

        };

        static int demoCounter = 0;
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task>> GetTask(bool? demo = null)
        {
            CheckRoleRecord(this.User.Identity.Name);

            if (demo == true)
            {
                var resd = demodata[demoCounter];
                demoCounter++;
                if (demoCounter >= demodata.Length)
                    demoCounter = 0;
                return await GetSmlouva(resd);
            }

            int tries = 0;
        start:
            var items = HlidacStatu.Entities.ItemToOcrQueue.TakeFromQueue(HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva, maxItems: 1);
            if (items == null)
                return StatusCode(404);
            if (items.Count() == 0)
                return StatusCode(404);
            ItemToOcrQueue item = items.First();
            HlidacStatu.DS.Api.OcrWork.DocTypes itemType = Enum.Parse<HlidacStatu.DS.Api.OcrWork.DocTypes>(item.ItemType);

            HlidacStatu.DS.Api.OcrWork.Task task = null;
            switch (itemType)
            {
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva:
                    task = await GetSmlouva(item);
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.VerejnaZakazka:
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Dataset:
                    break;
                case HlidacStatu.DS.Api.OcrWork.DocTypes.Insolvence:
                    break;
                default:
                    break;
            }
            if (task == null)
            {
                if (tries < 20)
                {
                    tries++;
                    goto start;
                }
                else
                    return StatusCode(404);
            }
            return task;
        }

        private async Task<HlidacStatu.DS.Api.OcrWork.Task> GetSmlouva(ItemToOcrQueue item)
        {
            var sml = await SmlouvaRepo.LoadAsync(item.ItemId, includePrilohy: false);

            if (sml == null)
                return null;
            HlidacStatu.DS.Api.OcrWork.Task res = new HlidacStatu.DS.Api.OcrWork.Task();
            res.taskId = item.Pk.ToString();
            res.type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva;
            res.parentDocId = item.ItemId;
            res.docs = sml.Prilohy.Select(m => new HlidacStatu.DS.Api.OcrWork.Task.Doc()
            {
                origFilename = m.nazevSouboru,
                prilohaId = m.UniqueHash(),
                url = m.odkaz
            }).ToArray();

            return res;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("GetMore")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task[]>> GetMore(int numberOfTasks)
        {
            CheckRoleRecord(this.User.Identity.Name);

            using HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task> q = new HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task>(
                HlidacStatu.DS.Api.TablesInDoc.TablesInDocProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            List<HlidacStatu.DS.Api.TablesInDoc.Task> tasks = new List<HlidacStatu.DS.Api.TablesInDoc.Task>();
            for (int i = 0; i < numberOfTasks; i++)
            {
                ulong? id = null;
                HlidacStatu.DS.Api.TablesInDoc.Task sq = q.GetAndAck(out id);
                if (sq == null)
                    break;
                tasks.Add(sq);
            }

            return null; //TODO

        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.OcrWork.Task res)
        {
            CheckRoleRecord(this.User.Identity.Name);
            if (res.type == HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva)
            {
                var done = await _saveSmlouva(res);
                ItemToOcrQueue.SetDone(int.Parse(res.taskId), true);
                return StatusCode(200);
            }
            return StatusCode(200);
        }

        private async Task<bool> _saveSmlouva(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            List<Smlouva.Priloha> newPrilohy = new List<Smlouva.Priloha>();
            bool changed = false;

            var sml = await SmlouvaRepo.LoadAsync(res.parentDocId);
            if (sml == null)
            {
                HlidacStatuApi.Code.Log.Logger.Error("Cannot save task {taskId} cannot find document {docId} of type {docType}", res.taskId, res.parentDocId, res.type);
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
                    HlidacStatuApi.Code.Log.Logger.Warning("Cannot find priloha {prilohaId} for smlouva {docId}", doc.prilohaId, res.parentDocId);
                    i = 0;
                    att = new Smlouva.Priloha();
                    newPrilohy.Add(att);

                }
                if (doc.result.Documents.Count > 1)
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

                    att.UpdateStatistics(sml);

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

                        att1.UpdateStatistics(sml);

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
                else if (doc.result.Documents.Count == 1)
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

                    att.UpdateStatistics(sml);

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
                _ = await SmlouvaRepo.SaveAsync(sml);
            }
            return true;
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {
            CheckRoleRecord(this.User.Identity.Name);

            HlidacStatuApi.Code.Log.Logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "TablesInDocsMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private static void CheckRoleRecord(string username)
        {
            return;
            //check if user is in blurredAPIAccess roles
            try
            {
                var found = HlidacStatu.Connectors.DirectDB.GetList<string, string>(
                    "select u.Id, ur.UserId from AspNetUsers u left join AspNetUserRoles ur on u.id = ur.UserId and ur.RoleId='e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' where u.UserName = @username",
                    System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                    );
                if (found.Count() == 0)
                    return;
                if (found.Count() == 1 && found.First().Item2 == null)
                {
                    HlidacStatu.Connectors.DirectDB.NoResult(
                        @"insert into AspNetUserRoles select  (select id from AspNetUsers where Email like @username) as userId,'e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' as roleId",
                        System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                        );
                }

            }
            catch (Exception e)
            {
                HlidacStatuApi.Code.Log.Logger.Error("cannot add {username} to the role blurredAPIAccess", e, username);
            }

        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(string id, bool force = true)
        {
            return await HlidacStatu.Lib.Data.External.Tables.TablesInDocs.Minion.CreateNewTaskAsync(id, force);
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Stats")]
        public async Task<ActionResult<BlurredPageAPIStatistics>> Stats()
        {
            using HlidacStatu.Q.Simple.Queue<BpTask> q = new HlidacStatu.Q.Simple.Queue<BpTask>(
                BlurredPageProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
            );


            DateTime now = DateTime.Now;
            var res = new BlurredPageAPIStatistics()
            {
                total = q.MessageCount()
            };

            return res;
        }



    }
}