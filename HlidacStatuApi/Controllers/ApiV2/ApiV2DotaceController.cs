using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{
    [SwaggerTag("Dotace")]
    [Route("api/v2/dotace")]
    public class ApiV2DotaceController : ControllerBase
    {
        /// <summary>
        /// Vyhledá dotace 
        /// </summary>
        /// <param name="dotaz">fulltext dotaz dle <a href="https://www.hlidacstatu.cz/napoveda">syntaxe</a> </param>
        /// <param name="strana">stránka, max. hodnota je 250</param>
        /// <param name="razeni">
        /// pořadí výsledků:<br />
        /// 0 Řadit podle relevance
        /// 1 Řadit podle data podpisu od nejnovějších
        /// 2 Řadit podle data podpisu od nejstarších
        /// 3 Řadit podle částky od největší
        /// 4 Řadit podle částky od nejmenší
        /// 5 Řadit podle IČO od největšího
        /// 6 Řadit podle IČO od nejmenšího
        /// </param>
        /// <returns></returns>
        [HttpGet("hledat")]
        [Authorize]
        public async Task<ActionResult<SearchResultDTO<Subsidy>>> Hledat([FromQuery] string? dotaz = null,
            [FromQuery] int? strana = null, [FromQuery] int? razeni = null)
        {
            if (strana is null || strana < 1)
                strana = 1;
            if (strana * ApiV2Controller.DefaultResultPageSize > ApiV2Controller.MaxResultsFromES)
            {
                return BadRequest(
                    $"Hodnota 'strana' nemůže být větší než {ApiV2Controller.MaxResultsFromES / ApiV2Controller.DefaultResultPageSize}");
            }

            if (string.IsNullOrWhiteSpace(dotaz))
            {
                return BadRequest($"Hodnota dotaz chybí.");
            }

            var result = await SubsidyRepo.Searching.SimpleSearchAsync(dotaz, strana.Value,
                ApiV2Controller.DefaultResultPageSize,
                (razeni ?? 0).ToString());

            if (result.IsValid == false)
            {
                return BadRequest($"Špatně nastavená hodnota dotaz=[{dotaz}]");
            }
            else
            {
                var filtered = result.ElasticResults.Hits
                    .Select(m => m.Source)
                    .ToArray();

                return new SearchResultDTO<Subsidy>(result.Total, result.Page, filtered);
            }
        }

        /// <summary>
        /// Vrátí detail jedné Subsidy.
        /// </summary>
        /// <param name="id">id Subsidy</param>
        /// <returns>detail Subsidy</returns>
        [HttpGet("{id?}")]
        [Authorize]
        public async Task<ActionResult<Subsidy>> Detail([FromRoute] string? id = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest($"Hodnota id chybí.");
            }

            var dotace = await SubsidyRepo.GetAsync(id);
            if (dotace == null)
            {
                return NotFound($"Dotace nenalezena");
            }

            return dotace;
        }
    }
}