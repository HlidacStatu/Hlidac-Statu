using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Models.Apiv2;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Authorization;

namespace HlidacStatu.Web.Controllers
{


    [SwaggerTag("Osoby")]

    [Route("api/v2/osoby")]
    public class ApiV2OsobyController : ApiV2AuthController
    {
        /// <summary>
        /// Vypíše detail osoby na základě "osobaId" parametru.
        /// </summary>
        /// <param name="osobaId">Id osoby v Hlídači Státu</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("{osobaId}")]
        public ActionResult<OsobaDetailDTO> Detail([FromRoute] string osobaId)
        {
            if (string.IsNullOrEmpty(osobaId))
            {
                return BadRequest("Hodnota osobaId chybí.");
            }

            var osoba = OsobaRepo.GetByNameId(osobaId);

            if (osoba == null)
            {
                return NotFound($"Osoba s id [{osobaId}] nenalezena");
            }

            OsobaDetailDTO OsobaDetail = new OsobaDetailDTO(osoba);

            return OsobaDetail;
        }

        [Authorize]
        [HttpGet, Route("hledatFtx")]
        public ActionResult<List<OsobaDTO>> OsobySearchFtx([FromQuery] string? ftxDotaz = null, [FromQuery] int? strana = null)
        {
            if (string.IsNullOrEmpty(ftxDotaz))
            {
                return BadRequest("Chybí query.");
            }

            if (strana is null || strana < 1)
                strana = 1;
            var osoby = OsobaRepo.Searching.SimpleSearchAsync(ftxDotaz, strana.Value, 30, OsobaRepo.Searching.OrderResult.Relevance);

            var result = osoby.Results.Select(o => new OsobaDTO(o)).ToList();

            return result;
        }

        [Authorize]
        [HttpGet, Route("hledat")]
        public ActionResult<List<OsobaDTO>> OsobySearch([FromQuery] string jmeno, [FromQuery] string prijmeni, [FromQuery] string datumNarozeni, [FromQuery] bool? ignoreDiakritiku = false)
        {
            if (string.IsNullOrEmpty(jmeno) || string.IsNullOrEmpty(prijmeni) || string.IsNullOrEmpty(datumNarozeni))
            {
                return BadRequest("Jmeno, prijmeni i datum narozeni jsou povinne.");
            }

            DateTime narozeni;
            if (DateTime.TryParseExact(datumNarozeni, "yyyy-MM-dd", HlidacStatu.Util.Consts.czCulture, System.Globalization.DateTimeStyles.AssumeLocal, out narozeni))
            {
                IEnumerable<Osoba> osoby = null;
                if (ignoreDiakritiku == true)
                    osoby = OsobaRepo.Searching.GetAllByNameAscii(jmeno, prijmeni, narozeni);
                else
                    osoby = OsobaRepo.Searching.GetAllByName(jmeno, prijmeni, narozeni);

                var result = osoby.Select(o => new OsobaDTO(o)).ToList();
                return result;
            }

            return BadRequest("Datum narozeni musi mit format yyyy-MM-dd.");
        }

        // /api/v2/osoby/social?typ=instagram&typ=twitter
        /// <summary>
        /// 
        /// </summary>
        /// <param name="typ">
        ///    Twitter = 0,
        ///    Facebook_page = 1,
        ///    Facebook_profile = 2,
        ///    Instagram = 3,
        ///    WWW =4,
        ///    Youtube = 5,
        ///    Zaznam_zastupitelstva = 6
        /// </param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("social")]
        public ActionResult<List<OsobaSocialDTO>> OsobySocial([FromQuery] OsobaEvent.SocialNetwork[] typ)
        {

            var socials = (typ is null || typ.Length == 0)
                ? Enum.GetNames(typeof(OsobaEvent.SocialNetwork))
                : typ.Select(t => t.ToString("G"));

            Expression<Func<OsobaEvent, bool>> socialNetworkFilter = e =>
                e.Type == (int)OsobaEvent.Types.SocialniSite
                && socials.Contains(e.Organizace);

            var osobaSocialDTOs = OsobaRepo.GetByEvent(socialNetworkFilter)
                .Select(o => new OsobaSocialDTO(o, socialNetworkFilter))
                .ToList();

            return osobaSocialDTOs;
        }

    }
}
