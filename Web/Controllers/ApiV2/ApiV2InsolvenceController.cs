using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Models.Apiv2;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System.Linq;
using System.Web;

namespace HlidacStatu.Web.Controllers
{
    [SwaggerTag("Insolvence")]
    [Route("api/v2/insolvence")]
    public class ApiV2InsolvenceController : ApiV2AuthController
    {
        /// <summary>
        /// Vyhledá smlouvy v databázi smluv
        /// </summary>
        /// <remarks>
        /// Toto API je pouze pro držitele komerční licence. Kontaktujte nás na api@hlidacstatu.cz.
        /// </remarks>
        /// <param name="dotaz">fulltext dotaz dle <a href="https://www.hlidacstatu.cz/napoveda">syntaxe</a> </param>
        /// <param name="strana">stránka, max. hodnota je 250</param>
        /// <param name="razeni">
        /// pořadí výsledků:<br />
        /// 0: podle relevance<br />
        /// 1: nově zahájené první
        /// 2: nově zveřejněné poslední
        /// 3: naposled změněné první
        /// 4: naposled změněné poslední
        /// </param>
        /// <returns></returns>
        [HttpGet("hledat")]
        [AuthorizeAndAudit]
        [SwaggerOperation(Tags = new[] { "Insolvence" })]
        public ActionResult<SearchResultDTO<Rizeni>> Hledat([FromQuery] string dotaz = null,
            [FromQuery] int? strana = null,
            [FromQuery] int? razeni = null)
        {
            strana = strana ?? 1;
            razeni = razeni ?? 0;

            if (strana < 1)
                strana = 1;
            if (strana * ApiV2Controller.DefaultResultPageSize > ApiV2Controller.MaxResultsFromES)
            {
                return BadRequest(
                    $"Hodnota 'strana' nemůže být větší než {ApiV2Controller.MaxResultsFromES / ApiV2Controller.DefaultResultPageSize}");
            }

            Repositories.Searching.InsolvenceSearchResult result = null;

            if (string.IsNullOrWhiteSpace(dotaz))
            {
                return BadRequest($"Hodnota dotaz chybí.");
            }

            bool isLimited = !(
                this.ApiAuth.ApiCall.UserRoles.Contains("novinar")
                || this.ApiAuth.ApiCall.UserRoles.Contains("Admin")
                || this.ApiAuth.ApiCall.UserRoles.Contains("KomercniLicence")
            );

            result = InsolvenceRepo.Searching.SimpleSearch(dotaz, strana.Value,
                ApiV2Controller.DefaultResultPageSize, razeni.Value, false, isLimited);

            if (result.IsValid == false)
            {
                return BadRequest($"Špatně nastavená hodnota dotaz=[{dotaz}]");
            }
            else
            {
                var filtered = result.ElasticResults.Hits
                    .Select(m => m.Source)
                    .ToArray();

                var ret = new SearchResultDTO<Rizeni>(result.Total, result.Page, filtered);
                return ret;
            }
        }

        /// <summary>
        /// Vrátí detail jedné smlouvy.
        /// </summary>
        /// <remarks>
        /// Toto API je pouze pro držitele komerční licence. Kontaktujte nás na api@hlidacstatu.cz.
        /// </remarks>
        /// <param name="id">id smlouvy</param>
        /// <returns>detail smlouvy</returns>
        [HttpGet("{id?}")]
        [AuthorizeAndAudit]
        public ActionResult<Rizeni> Detail([FromRoute] string? id = null)
        {
            id = HttpUtility.UrlDecode(id);
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest($"Hodnota id chybí.");
            }

            bool isLimited = !(
                this.ApiAuth.ApiCall.UserRoles.Contains("novinar")
                || this.ApiAuth.ApiCall.UserRoles.Contains("Admin")
                || this.ApiAuth.ApiCall.UserRoles.Contains("KomercniLicence")
            );

            var ins = InsolvenceRepo.LoadFromES(id, true, isLimited);
            if (ins == null)
            {
                return NotFound($"Insolvence nenalezena");
            }

            return ins.Rizeni;
        }
    }
}