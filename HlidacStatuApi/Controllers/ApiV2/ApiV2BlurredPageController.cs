

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

        static System.Collections.Concurrent.ConcurrentDictionary<string, DateTime?> idsToProcess = null;
        static object lockObj = new object();
        static ApiV2BlurredPageController()
        {
            idsToProcess = new System.Collections.Concurrent.ConcurrentDictionary<string, DateTime?>(
                SmlouvaRepo.AllIdsFromDB().Distinct().Select(m => new KeyValuePair<string, DateTime?>(m, null))
                );
        }

        // /api/v2/{id}
        [Authorize]
        [HttpGet("Get")]
        public async Task<ActionResult<BpGet>> Get([FromRoute] string text)
        {
            string nextId = null;
            DateTime now = DateTime.Now;
            lock (lockObj)
            {
                nextId = idsToProcess.FirstOrDefault(m =>
                    m.Value == null
                    || (m.Value.HasValue && (now - m.Value.Value).TotalMinutes > 60)
                ).Key;
                if (nextId == null)
                    return StatusCode(404);
                else
                    idsToProcess[nextId] = DateTime.Now;
            }

            var sml = await SmlouvaRepo.LoadAsync(nextId, includePrilohy: false);
            if (sml == null)
                return StatusCode(404);

            var res = new BpGet();
            res.SmlouvaId = nextId;
            res.Prilohy = sml.Prilohy
                        .Select(m => new BpGet.BpGPriloha()
                        {
                            UniqueId = m.UniqueHash(),
                            Url = m.GetUrl(nextId, true)
                        }
                        )
                        .ToArray();

            return res;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<DSCreatedDTO>> Save([FromBody] BpSave data)
        {
            List<Task> tasks = new List<Task>();
            foreach (var p in data.Prilohy)
            {
                foreach (var page in p.Pages)
                {
                    PageMetadata pm = new PageMetadata();
                    pm.SmlouvaId = data.SmlouvaId;
                    pm.PrilohaId = p.UniqueId;
                    pm.PageNum = page.Page;
                    pm.Blurred = new PageMetadata.BlurredMetadata()
                    {
                        BlackenAreaBoundaries = page.BlackenAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.X,
                                Y = b.Y,
                                Width = b.Width,
                                Height = b.Height
                            }
                            ).ToArray(),
                        TextAreaBoundaries = page.TextAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.X,
                                Y = b.Y,
                                Width = b.Width,
                                Height = b.Height
                            }
                            ).ToArray(),
                        AnalyzerVersion = page.AnalyzerVersion,
                        Created = DateTime.Now,
                        ImageWidth = page.ImageWidth,
                        ImageHeight = page.ImageHeight,
                        BlackenArea = page.BlackenArea,
                        TextArea = page.TextArea
                    };
                    var t = PageMetadataRepo.SaveAsync(pm);
                    tasks.Add(t);
                    //t.Wait();
                }
            }
            Task.WaitAll(tasks.ToArray());
            return StatusCode(200);
        }



        public class BpGet
        {
            public class BpGPriloha
            {
                public string UniqueId { get; set; }
                public string Url { get; set; }
            }
            public string SmlouvaId { get; set; }
            public BpGPriloha[] Prilohy { get; set; }

        }

        public class BpSave
        {
            public string SmlouvaId { get; set; }

            public BpSPriloha[] Prilohy { get; set; }

            public class BpSPriloha
            {
                public string UniqueId { get; set; }
                public PageMetadata[] Pages { get; set; }
            }
            public class PageMetadata
            {
                public class Boundary
                {
                    public Boundary() { }


                    public int X { get; set; }
                    public int Y { get; set; }
                    public int Width { get; set; }
                    public int Height { get; set; }


                }

                public int Page { get; set; }

                public int ImageWidth { get; set; }
                public int ImageHeight { get; set; }

                public long TextArea { get; set; }
                public long BlackenArea { get; set; }

                [Nest.Date]
                public DateTime Created { get; set; }

                public string AnalyzerVersion { get; set; }

                public Boundary[] TextAreaBoundaries { get; set; }

                public Boundary[] BlackenAreaBoundaries { get; set; }


            }

        }

    }
}