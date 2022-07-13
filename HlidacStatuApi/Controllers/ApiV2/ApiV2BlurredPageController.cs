using System.ComponentModel;
using System.Net;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{


    [SwaggerTag("BlurredPage")]
    [ApiController]
    [Route("api/v2/bp")]
    public class ApiV2BlurredPageController : ControllerBase
    {
        // /api/v2/{id}
        [Authorize]
        [HttpGet("Get")]
        public ActionResult<BpGet> Get([FromRoute] string text)
        {
            return new BpGet(); ;
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpPost]
        public async Task<ActionResult<DSCreatedDTO>> Save([FromBody] BpSave data)
        {
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