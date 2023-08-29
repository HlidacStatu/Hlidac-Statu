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



        static bool demoswitcher = false;
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task>> GetTask()
        {
            CheckRoleRecord(this.User.Identity.Name);

            demoswitcher = !demoswitcher;
            if (demoswitcher)
                return new HlidacStatu.DS.Api.OcrWork.Task()
                {
                    taskId = "0",
                    parentDocId = "0",
                    type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva,
                    docs = new HlidacStatu.DS.Api.OcrWork.Task.Doc[]{
                        new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                        {
                            prilohaId = "00",
                            url = "http://zapisnikzmizeleho.cz/wp-content/themes/zapisnikz1.1/img-zapisnik/velikonocni-festival-brno-2011-04/stabat-mater-program.gif",
                            origFilename = "stabat-mater-program.gif"
                        }
                    }
                };
            else
                return new HlidacStatu.DS.Api.OcrWork.Task()
                {
                    taskId = "00",
                    parentDocId = "1",
                    type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva,
                    docs = new HlidacStatu.DS.Api.OcrWork.Task.Doc[]{
                        new HlidacStatu.DS.Api.OcrWork.Task.Doc()
                        {
                            prilohaId = "1",
                            url = "https://www.hlidacstatu.cz/KopiePrilohy/4288264?hash=d702c02db57b151eb685c7923d673b3709710b01f1e0050add51d8d1569189a9",
                            origFilename = "DRNP2012VC1485D2.pdf"
                        }
                    }
                };

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
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.OcrWork.Task res)
        {
            CheckRoleRecord(this.User.Identity.Name);
            if (res.type == HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva)
            {
                return await _saveSmlouva(res);
            }
            return StatusCode(200);
        }

        private async Task<ActionResult> _saveSmlouva(HlidacStatu.DS.Api.OcrWork.Task res)
        {
            List<Smlouva.Priloha> newPrilohy = new List<Smlouva.Priloha>();
            bool changed = false;

            var sml = await SmlouvaRepo.LoadAsync(res.parentDocId);
            if (sml == null)
                return StatusCode(404);

            foreach (var doc in res.docs)
            {
                var attIdx = sml.Prilohy
                        .Select((pr, i) => new { i, pr })
                        .Where(m => m.pr.UniqueHash() == doc.prilohaId)
                        .FirstOrDefault();
                if (attIdx == null)
                    return StatusCode(404);
                int i = attIdx.i;
                Smlouva.Priloha att = attIdx.pr;
                if (doc.result.Documents.Count > 1)
                {
                    //first
                    att.PlainTextContent = HlidacStatu.Util.ParseTools.NormalizePrilohaPlaintextText(doc.result.Documents[0].Text);
                    if (doc.result.Documents[0].UsedOCR)
                        att.PlainTextContentQuality = DataQualityEnum.Estimated;
                    else
                        att.PlainTextContentQuality = DataQualityEnum.Parsed;

                    att.ContentType = doc.result.Documents[0].ContentType;

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
            return StatusCode(200);
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