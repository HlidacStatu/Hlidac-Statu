﻿using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatuApi.Controllers.ApiV2
{
    [Route("api/v2/kindex")]
    public class ApiV2KindexController : ControllerBase
    {

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet]
        public ActionResult<IEnumerable<SubjectNameCache>> AllSubjects()
        {
            return SubjectNameCache.GetCompanies().Values;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("full/{ico}")]
        public async Task<ActionResult<KIndexData>> FullDetail([FromRoute] string ico)
        {
            if (string.IsNullOrEmpty(ico))
            {
                return BadRequest($"Hodnota ico chybí.");
            }

            var kindex = await KIndex.GetAsync(ico);

            if (kindex == null)
            {
                return NotFound($"Kindex pro ico [{ico}] nenalezen.");
            }

            return kindex;

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("{ico}")]
        public async Task<ActionResult<KIndexDTO>> Detail([FromRoute] string ico)
        {
            if (string.IsNullOrEmpty(ico))
            {
                return BadRequest($"Hodnota ico chybí.");
            }

            var kindex = await KIndex.GetAsync(ico);

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