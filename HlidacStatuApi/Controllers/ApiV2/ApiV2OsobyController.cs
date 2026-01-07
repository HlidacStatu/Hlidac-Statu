using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Linq.Expressions;

namespace HlidacStatuApi.Controllers.ApiV2
{


    [SwaggerTag("Osoby")]

    [Route("api/v2/osoby")]
    public class ApiV2OsobyController : ControllerBase
    {
        /// <summary>
        /// Vypíše detail osoby na základě "osobaId" parametru.
        /// </summary>
        /// <param name="osobaId">Id osoby v Hlídači Státu</param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("{osobaId}")]
        public async Task<ActionResult<OsobaDetailDTO>> Detail([FromRoute] string osobaId)
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

            return await OsobaDetailDTO.CreateOsobaDetailDTOAsync(osoba);
        }

        /// <summary>
        /// Vyhledání osoby podle jména v textu
        /// </summary>
        /// <param name="ftxDotaz">Jméno osoby</param>
        /// <param name="status"> 
        /// 0 - nepolitická osoba (dostupné pouze pro uživatele s komerční licencí)
        /// 1 - sponzoři polit.stran, politici, úředníci
        /// </param>
        /// <param name="strana"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet, Route("hledatFtx")]
        public async Task<ActionResult<List<OsobaDTO>>> OsobySearchFtx([FromQuery] string? ftxDotaz = null, [FromQuery] int status = 1, [FromQuery] int? strana = null)
        {
            if (string.IsNullOrEmpty(ftxDotaz))
            {
                return BadRequest("Chybí query.");
            }

            if (strana is null || strana < 1)
                strana = 1;
            if (status == 1)
                status = -1;

            if (status == 0
                && !(this.User.IsInRole("Admin") || User.IsInRole("KomercniLicence"))
                )
            {
                return Unauthorized("Je potreba komercni licence. viz https://texty.hlidacstatu.cz/licence/");
            }


            var osoby = await OsobaRepo.Searching.SimpleSearchAsync(ftxDotaz,
                strana.Value, 30, OsobaRepo.Searching.OrderResult.Relevance, osobaStatus: status);

            var result = osoby.Result.Select(o => new OsobaDTO(o)).ToList();

            return result;
        }


        [Authorize(Roles = "TeamMember")]
        [HttpGet, Route("PolitikFromText")]
        public async Task<ActionResult> PolitikFromText(string text)
        {
            var oo = await OsobaRepo.Searching.GetFirstPolitikFromTextAsync(text);

            if (oo != null)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    new { osobaid = oo.NameId, jmeno = oo.Jmeno, prijmeni = oo.Prijmeni, politickaStrana = oo.CurrentPoliticalPartyAsync() }
                ), "application/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content("{}", "application/json");
            }
        }

        [Authorize(Roles = "TeamMember")]
        [HttpGet, Route("PoliticiFromText")]
        public async Task<ActionResult> PoliticiFromText(string text)
        {
            var oo = await OsobaRepo.Searching.GetBestPoliticiFromTextAsync(text);

            if (oo != null)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    oo.Select(o => new { osobaid = o.NameId, jmeno = o.Jmeno, prijmeni = o.Prijmeni, politickaStrana = o.CurrentPoliticalPartyAsync() })
                        .ToArray()
                ), "application/json", System.Text.Encoding.UTF8);
            }
            else
            {
                return Content("[]", "application/json");
            }
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
        public ActionResult<List<OsobaSocialDTO>> OsobySocial([FromQuery] OsobaEvent.SocialNetwork typ)
        {

            string social = typ.ToString();
            Expression<Func<OsobaEvent, bool>> socialNetworkFilter = e =>
                e.Type == (int)OsobaEvent.Types.SocialniSite
                && e.Organizace == social;


            var osobaSocialDTOs = OsobaRepo.GetByEvent(socialNetworkFilter)
                .Select(o => new OsobaSocialDTO(o, socialNetworkFilter))
                .ToList();

            return osobaSocialDTOs;
        }

    }
}
