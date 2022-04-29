using HlidacStatu.Entities.Dotace;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Models.Apiv2;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace HlidacStatu.Web.Controllers
{
    [SwaggerTag("Dotace")]
    [Route("api/v2/dotace")]
    public class ApiV2DotaceController : ApiV2AuthController
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
        public ActionResult<SearchResultDTO<Dotace>> Hledat([FromQuery] string? dotaz = null, [FromQuery] int? strana = null, [FromQuery] int? razeni = null)
        {
            if (strana is null || strana < 1)
                strana = 1;
            if (strana * ApiV2Controller.DefaultResultPageSize > ApiV2Controller.MaxResultsFromES)
            {
                return BadRequest($"Hodnota 'strana' nemůže být větší než {ApiV2Controller.MaxResultsFromES / ApiV2Controller.DefaultResultPageSize}");
            }

            Repositories.Searching.DotaceSearchResult result = null;

            if (string.IsNullOrWhiteSpace(dotaz))
            {
                return BadRequest($"Hodnota dotaz chybí.");
            }


            result = DotaceRepo.Searching.SimpleSearchAsync(dotaz, strana.Value,
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

                return new SearchResultDTO<Dotace>(result.Total, result.Page, filtered);
            }
        }

        /// <summary>
        /// Vrátí detail jedné dotace.
        /// </summary>
        /// <param name="id">id dotace</param>
        /// <returns>detail dotace</returns>
        [HttpGet("{id?}")]
        [Authorize]
        public ActionResult<Dotace> Detail([FromRoute] string? id = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest($"Hodnota id chybí.");
            }

            var dotace = DotaceRepo.GetAsync(id);
            if (dotace == null)
            {
                return NotFound($"Dotace nenalezena");
            }
            return dotace;
        }



    }
}
