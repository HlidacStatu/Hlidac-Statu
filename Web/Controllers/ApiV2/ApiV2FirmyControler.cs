using System;
using System.Linq;
using System.Data;
using System.Net.Http;
using System.Collections.Generic;
using HlidacStatu.Entities;
using System.Linq.Expressions;
using HlidacStatu.Datasets;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Models.Apiv2;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatu.Web.Controllers
{

    [SwaggerTag("Firmy")]

    [Route("api/v2/firmy")]
    public class ApiV2FirmyController : ApiV2AuthController
    {

        /// <summary>
        /// Vyhledá firmu v databázi Hlídače státu.
        /// </summary>
        /// <param name="ico">ico firmy</param>
        /// <returns>Ico, jméno a datová schránka</returns>
        [AuthorizeAndAudit]
        [HttpGet("ico/{ico}")]
        public ActionResult<FirmaDTO> CompanyPerIco([FromRoute]string ico)
        {
            try
            {
                var f = Firmy.Get(ico);
                if (f.Valid == false)
                {
                    return NotFound($"Firma {ico} nenalezena.");
                }
                else
                    return new FirmaDTO()
                    {
                        ICO = f.ICO,
                        Jmeno = f.Jmeno,
                        DatoveSchranky = f.DatovaSchranka
                    };

            }
            catch (DataSetException dse)
            {
                return BadRequest($"{dse.APIResponse.error.description}");
            }
            catch (Exception ex)
            {
                Util.Consts.Logger.Error("Dataset API", ex);
                return BadRequest($"Obecná chyba - {ex.Message}");
            }
        }

        /// <summary>
        /// Vyhledá firmu v databázi Hlídače státu.
        /// </summary>
        /// <param name="jmenoFirmy">název firmy</param>
        /// <returns>Ico, jméno a datová schránka</returns>
        [AuthorizeAndAudit]
        [HttpGet("{jmenoFirmy}")]
        public ActionResult<FirmaDTO> CompanyID([FromRoute]string jmenoFirmy)
        {
            try
            {
                if (string.IsNullOrEmpty(jmenoFirmy))
                {
                    return BadRequest($"Hodnota companyName chybí.");
                }
                else
                {
                    var name = Firma.JmenoBezKoncovky(jmenoFirmy);
                    var found = FirmaRepo.Searching.FindAll(name, 1).FirstOrDefault();
                    if (found == null)
                    {
                        return NotFound($"Firma {jmenoFirmy} nenalezena.");
                    }
                    else
                        return new FirmaDTO()
                        {
                            ICO = found.ICO,
                            Jmeno = found.Jmeno,
                            DatoveSchranky = found.DatovaSchranka
                        };
                }
            }
            catch (DataSetException dse)
            {
                return BadRequest($"{dse.APIResponse.error.description}");
            }
            catch (Exception ex)
            {
                Util.Consts.Logger.Error("Dataset API", ex);
                return BadRequest($"Obecná chyba - {ex.Message}");
            }
        }

        [AuthorizeAndAudit(Roles = "KomercniLicence,PrivateApi,Admin")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("vsechny")]
        public ActionResult<HttpResponseMessage> Vsechny()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024 * 500);
            string cnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
            using (Devmasters.PersistLib p = new Devmasters.PersistLib())
            {

                var reader = p.ExecuteReader(cnnStr, CommandType.Text, "select ico, jmeno from firma where ISNUMERIC(ico) = 1", null);
                while (reader.Read())
                {
                    string ico = reader.GetString(0).Trim();
                    string name = reader.GetString(1).Trim();
                    if (!string.IsNullOrWhiteSpace(ico)
                        && !string.IsNullOrWhiteSpace(name)
                        && Devmasters.TextUtil.IsNumeric(ico))
                    {
                        ico = Util.ParseTools.NormalizeIco(ico);
                        sb.AppendLine(ico + "\t" + name);
                    }
                }
            }

            var res = new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(sb.ToString()) };
            return res;
        }

        // /api/v2/firmy/social?typ=WWW&typ=Youtube
        [AuthorizeAndAudit]
        [HttpGet("social")]
        public ActionResult<List<FirmaSocialDTO>> Social([FromQuery] OsobaEvent.SocialNetwork[] typ)
        {

            var socials = (typ is null || typ.Length == 0)
                ? Enum.GetNames(typeof(OsobaEvent.SocialNetwork))
                : typ.Select(t => t.ToString("G"));

            Expression<Func<OsobaEvent, bool>> socialNetworkFilter = e =>
                e.Type == (int)OsobaEvent.Types.SocialniSite
                && e.Ico.Length == 8
                && socials.Contains(e.Organizace);

            var events = OsobaEventRepo.GetByEvent(socialNetworkFilter)
                .GroupBy(e => e.Ico)
                .Select(g => TransformEventsToFirmaSocial(g))
                .Where(r => r != null)
                .ToList();

            return events;

        }

        private FirmaSocialDTO TransformEventsToFirmaSocial(IGrouping<string, OsobaEvent> groupedEvents)
        {
            var firma = FirmaRepo.FromIco(groupedEvents.Key);
            if (firma is null || !firma.Valid)
            {
                return null;
            }

            return new FirmaSocialDTO(firma, groupedEvents.ToList());
        }
    }
}