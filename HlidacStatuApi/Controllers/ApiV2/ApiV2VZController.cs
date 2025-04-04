﻿using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using ILogger = Serilog.ILogger;

namespace HlidacStatuApi.Controllers.ApiV2
{
    [SwaggerTag("Veřejné zakázky")]
    [Route("api/v2/verejnezakazky")]
    public class ApiV2VZController : ControllerBase
    {
        
        private readonly ILogger _logger = Serilog.Log.ForContext<ApiV2VZController>();

        /// <summary>
        /// Detail veřejné zakázky
        /// </summary>
        /// <remarks>
        /// Toto API je pouze pro držitele komerční licence. Kontaktujte nás na api@hlidacstatu.cz.
        /// </remarks>
        /// <param name="id">Id veřejné zakázky</param>
        /// <returns>detail veřejné zakázky</returns>
        [Authorize(Roles = "Admin,KomercniLicence")]
        [HttpGet("{id?}")]
        public async Task<ActionResult<VerejnaZakazka>> Detail([FromRoute] string? id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest($"Hodnota id chybí.");
            }

            var zakazka = await VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (zakazka == null)
            {
                return NotFound($"Zakazka nenalezena");
            }

            return zakazka;
        }

        /// <summary>
        /// Vyhledá veřejné zakázky v databázi Hlídače smluv
        /// </summary>
        /// <remarks>
        /// Toto API je pouze pro držitele komerční licence. Kontaktujte nás na api@hlidacstatu.cz.
        /// </remarks>
        /// <param name="dotaz">fulltext dotaz dle <a href="https://www.hlidacstatu.cz/napoveda">syntaxe</a></param>
        /// <param name="strana">stránka, max. hodnota je 250</param>
        /// <param name="razeni">
        /// pořadí výsledků: <br />
        /// 0: podle relevance<br />
        /// 1: nově zveřejněné první<br />
        /// 2: nově zveřejněné poslední<br />
        /// 3: nejlevnější první<br />
        /// 4: nejdražší první<br />
        /// 5: nově uzavřené první<br />
        /// 6: nově uzavřené poslední<br />
        /// 8: podle odběratele<br />
        /// 9: podle dodavatele<br />
        /// </param>
        /// <returns>nalezené veřejné zakázky</returns>
        [Authorize(Roles = "Admin,KomercniLicence")]
        [HttpGet("hledat")]
        public async Task<ActionResult<SearchResultDTO<VerejnaZakazka>>> Hledat([FromQuery] string? dotaz = null, [FromQuery] int? strana = null, [FromQuery] int? razeni = null)
        {
            strana = strana ?? 1;
            razeni = razeni ?? 0;
            if (strana < 1)
                strana = 1;
            if (strana * ApiV2Controller.DefaultResultPageSize > ApiV2Controller.MaxResultsFromES)
            {
                return BadRequest($"Hodnota 'strana' nemůže být větší než {ApiV2Controller.MaxResultsFromES / ApiV2Controller.DefaultResultPageSize}");
            }

            if (string.IsNullOrWhiteSpace(dotaz))
            {
                return BadRequest($"Hodnota dotaz chybí.");
            }

            var result = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(dotaz, null, strana.Value,
                ApiV2Controller.DefaultResultPageSize,
                razeni.Value.ToString());

            if (result.IsValid == false)
            {
                return BadRequest($"Špatně nastavená hodnota dotaz=[{dotaz}]");
            }
            else
            {
                var zakazky = result.ElasticResults.Hits
                    .Select(m => m.Source).ToArray();

                return new SearchResultDTO<VerejnaZakazka>(result.Total, result.Page, zakazky);
            }
        }
        
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpGet("hledatids")]
        public async Task<ActionResult<List<string>>> HledatIds([FromQuery] int year)
        {
            try
            {
                return await VerejnaZakazkaRepo.Searching.GetAllIdsAsync(year);
            }
            catch (Exception e)
            {
                _logger.Error(e, "HledatIds failed during searching");
                return StatusCode(500, $"error {e}");
            }

        }
    }
}
