﻿using System.Data;
using System.Linq.Expressions;
using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("Firmy")]

    [Route("api/v2/firmy")]
    public class ApiV2FirmyController : ControllerBase
    {
        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ApiV2FirmyController>();

        /// <summary>
        /// Vyhledá firmu v databázi Hlídače státu.
        /// </summary>
        /// <param name="ico">ico firmy</param>
        /// <returns>Ico, jméno a datová schránka</returns>
        [Authorize]
        [HttpGet("ico/{ico}")]
        public ActionResult<FirmaDTO> CompanyPerIco([FromRoute] string ico)
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
                _logger.Error(ex, "Dataset API");
                return BadRequest($"Obecná chyba - {ex.Message}");
            }
        }

        /// <summary>
        /// Vyhledá firmu v databázi Hlídače státu.
        /// </summary>
        /// <param name="jmenoFirmy">název firmy</param>
        /// <returns>Ico, jméno a datová schránka</returns>
        [Authorize]
        [HttpGet("{jmenoFirmy}")]
        public async Task<ActionResult<FirmaDTO>> CompanyID([FromRoute] string jmenoFirmy)
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
                    var found = (await FirmaRepo.Searching.FindAllAsync(name, 1)).FirstOrDefault();
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
                _logger.Error(ex, "Dataset API");
                return BadRequest($"Obecná chyba - {ex.Message}");
            }
        }

        [Authorize(Roles = "KomercniLicence,PrivateApi,Admin")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("vsechny")]
        public ActionResult<string> Vsechny()
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
                        ico = HlidacStatu.Util.ParseTools.NormalizeIco(ico);
                        sb.AppendLine(ico + "\t" + name);
                    }
                }
            }

            return sb.ToString();
        }

        // /api/v2/firmy/social?typ=WWW&typ=Youtube
        [Authorize]
        [HttpGet("social")]
        public ActionResult<List<FirmaSocialDTO>> Social([FromQuery] OsobaEvent.SocialNetwork typ)
        {

            var social = typ.ToString("G");

            Expression<Func<OsobaEvent, bool>> socialNetworkFilter = e =>
                e.Type == (int)OsobaEvent.Types.SocialniSite
                && e.Organizace == social;

            var events = OsobaEventRepo.GetByEvent(socialNetworkFilter)
                .GroupBy(e => e.Ico)
                .Select(g => TransformEventsToFirmaSocial(g))
                .Where(r => r != null)
                .ToList();

            return events;

        }

        /// <summary>
        /// Vrátí detailní informace o firmách na základě IČO nebo názvů firem.
        /// 
        /// </summary>
        /// <param name="icos">IČO nebo seznam IČO oddělený | (pipe)</param>
        /// <param name="names">Jmeno firmy nebo seznam jmen firem oddělený znakem '|' (pipe)</param>
        /// <returns></returns>
        [Authorize(Roles = "Admin")]
        [HttpGet("GetDetailInfo")]
        public ActionResult<List<HlidacStatu.DS.Api.Firmy.SubjektDetailInfo>> GetDetailInfo([FromQuery] string icos, [FromQuery] string names)
        {
            List<HlidacStatu.DS.Api.Firmy.SubjektDetailInfo> res = new List<HlidacStatu.DS.Api.Firmy.SubjektDetailInfo>();

            if (!string.IsNullOrEmpty(icos))
            {
                var icosList = icos.Split('|').Select(i => i.Trim()).ToList();
                foreach (var ico in icosList)
                {
                    var inf = HlidacStatu.Repositories.FirmaRepo.GetDetailInfo(ico,"");
                    if (inf != null)
                    {
                        res.Add(inf);
                    }   
                }
            }
            if (!string.IsNullOrEmpty(names))
            {
                var namesList = names.Split('|').Select(i => i.Trim()).ToList();
                foreach (var nam in namesList)
                {
                    var inf = HlidacStatu.Repositories.FirmaRepo.GetDetailInfo("", nam);
                    if (inf != null)
                    {
                        res.Add(inf);
                    }
                }
            }

            return res;
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