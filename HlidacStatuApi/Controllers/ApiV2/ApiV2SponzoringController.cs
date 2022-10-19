using HlidacStatu.Entities.Views;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{


    [SwaggerTag("Sponzoring")]

    [Route("api/v2/sponzoring")]
    public class ApiV2SponzoringController : ControllerBase
    {
        /// <summary>
        /// Vypíše seznam darů obdržených politickou stranou. 
        /// </summary>
        /// <param name="icoPrijemce">Ičo politické strany, která obdržela dar</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("{icoPrijemce}")]
        public async Task<ActionResult<List<SponzoringDetail>>> Detail([FromRoute] string icoPrijemce, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(icoPrijemce))
            {
                return BadRequest($"Hodnota {nameof(icoPrijemce)} chybí.");
            }

            var sponzoring = await SponzoringRepo.GetByPrijemceWithPersonDetailsAsync(icoPrijemce, cancellationToken);

            return sponzoring;
        }

        

    }
}
