

using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using HlidacStatuApi.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{


    [SwaggerTag("BlurredPage")]
    [ApiController]
    [Route("api/v2/bp")]
    public class ApiV2BlurredPageController : ControllerBase
    {
        private class processed
        {
            public DateTime taken { get; set; } = DateTime.MinValue;
            public string takenByUser { get; set; } = null;
        }
        static System.Collections.Concurrent.ConcurrentDictionary<string, processed> idsToProcess = null;
        static object lockObj = new object();
        static ApiV2BlurredPageController()
        {
            idsToProcess = new System.Collections.Concurrent.ConcurrentDictionary<string, processed>(
                SmlouvaRepo.AllIdsFromDB().Distinct().Select(m => new KeyValuePair<string, processed>(m, null))
                );
        }

        // /api/v2/{id}
        [Authorize]
        [HttpGet("Get")]
        public async Task<ActionResult<BpGet>> Get()
        {
            string nextId = null;
            DateTime now = DateTime.Now;
again:
            lock (lockObj)
            {
                nextId = idsToProcess.FirstOrDefault(m =>
                    m.Value == null
                    || (m.Value!=null && (now - m.Value.taken).TotalMinutes > 60)
                ).Key;
                if (nextId == null)
                    return StatusCode(404);
                else if (idsToProcess.ContainsKey(nextId))
                    idsToProcess[nextId] = new processed() { taken = DateTime.Now, takenByUser = HttpContext.User?.Identity?.Name };
                else
                    goto again;
            }

            if (await PageMetadataRepo.ExistsInPageMetadata(nextId))
            {
                idsToProcess.Remove(nextId, out var dt);
                goto again;
            }

            var sml = await SmlouvaRepo.LoadAsync(nextId, includePrilohy: false);
            if (sml == null)
                return StatusCode(404);

            var res = new BpGet();
            res.smlouvaId = nextId;
            res.prilohy = sml.Prilohy
                        .Select(m => new BpGet.BpGPriloha()
                        {
                            uniqueId = m.UniqueHash(),
                            url = m.GetUrl(nextId, false)
                        }
                        )
                        .ToArray();

            return res;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost("Save")]
        public async Task<ActionResult<DSCreatedDTO>> Save([FromBody] BpSave data)
        {
            List<Task> tasks = new List<Task>();
            foreach (var p in data.prilohy)
            {
                foreach (var page in p.pages)
                {
                    PageMetadata pm = new PageMetadata();
                    pm.SmlouvaId = data.smlouvaId;
                    pm.PrilohaId = p.uniqueId;
                    pm.PageNum = page.page;
                    pm.Blurred = new PageMetadata.BlurredMetadata()
                    {
                        BlackenAreaBoundaries = page.blackenAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        TextAreaBoundaries = page.textAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        AnalyzerVersion = page.analyzerVersion,
                        Created = DateTime.Now,
                        ImageWidth = page.imageWidth,
                        ImageHeight = page.imageHeight,
                        BlackenArea = page.blackenArea,
                        TextArea = page.textArea
                    };
                    var t = PageMetadataRepo.SaveAsync(pm);
                    tasks.Add(t);
                    //t.Wait();
                }
            }
            Task.WaitAll(tasks.ToArray());
            idsToProcess.Remove(data.smlouvaId, out var dt);
            return StatusCode(200);
        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("Stats")]
        public async Task<ActionResult<Statistics>> Stats()
        {
            DateTime now = DateTime.Now;

            var res = new Statistics()
            {
                total = idsToProcess.Count,
                totalTaken = idsToProcess.Count(m=>m.Value != null),
                totalFailed = idsToProcess.Count(m=>m.Value != null && (now - m.Value.taken).TotalMinutes > 60)
            };

            return res;
        }

        public class Statistics
        {
            public int total { get;  set; }
            public int totalTaken { get; set; }
            public int totalFailed { get; set; }
        }


        public class BpGet
        {
            public class BpGPriloha
            {
                public string uniqueId { get; set; }
                public string url { get; set; }
            }
            public string smlouvaId { get; set; }
            public BpGPriloha[] prilohy { get; set; }

        }

        public class BpSave
        {
            public string smlouvaId { get; set; }

            public BpSPriloha[] prilohy { get; set; }

            public class BpSPriloha
            {
                public string uniqueId { get; set; }
                public PageMetadata[] pages { get; set; }
            }
            public class PageMetadata
            {
                public class Boundary
                {
                    public Boundary() { }


                    public int x { get; set; }
                    public int y { get; set; }
                    public int width { get; set; }
                    public int height { get; set; }


                }

                public int page { get; set; }

                public int imageWidth { get; set; }
                public int imageHeight { get; set; }

                public long textArea { get; set; }
                public long blackenArea { get; set; }

                public DateTime created { get; set; }

                public string analyzerVersion { get; set; }

                public Boundary[] textAreaBoundaries { get; set; }

                public Boundary[] blackenAreaBoundaries { get; set; }


            }

        }

    }
}