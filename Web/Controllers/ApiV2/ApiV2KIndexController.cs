using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Web.Models.Apiv2;

using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    [Route("api/v2/kindex")]
    public class ApiV2KindexController : ApiV2AuthController
    {

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        public ActionResult<IEnumerable<SubjectNameCache>> AllSubjects()
        {
            return SubjectNameCache.GetCompanies().Values;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("full/{ico}")]
        public ActionResult<KIndexData> FullDetail([FromRoute] string ico)
        {
            if (string.IsNullOrEmpty(ico))
            {
                return BadRequest($"Hodnota ico chybí.");
            }

            var kindex = KIndex.Get(ico);

            if (kindex == null)
            {
                return NotFound($"Kindex pro ico [{ico}] nenalezen.");
            }

            return kindex;

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("{ico}")]
        public ActionResult<KIndexDTO> Detail([FromRoute] string ico)
        {
            if (string.IsNullOrEmpty(ico))
            {
                return BadRequest($"Hodnota ico chybí.");
            }

            var kindex = KIndex.Get(ico);

            if (kindex == null)
            {
                return NotFound($"Kindex pro ico [{ico}] nenalezen.");
            }

            var limitedKindex = new KIndexDTO
            {
                Ico = kindex.Ico,
                Name = kindex.Jmeno,
                LastChange = kindex.LastSaved,
                AnnualCalculations = kindex.roky.Select(annual => new KIndexYearsDTO
                {
                    KIndex = annual.KIndex,
                    Calculation = annual.KIndexVypocet
                })
            };

            return limitedKindex;
        }

    }
}