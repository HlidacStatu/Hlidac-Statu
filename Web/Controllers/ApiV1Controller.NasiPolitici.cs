using HlidacStatu.Datasets;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Util;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace HlidacStatu.Web.Controllers
{
    public partial class ApiV1Controller : Controller
    {
        class StatementDTO
        {
            public string Id { get; set; }
            public string Type { get; set; }
            public DateTime? FromDate { get; set; }
            public DateTime? ToDate { get; set; }
            public dynamic LegalBusinessAssociates { get; set; }
            public dynamic OrganizationMember { get; set; }
            public string Visibility { get; set; }
        }

        [Authorize]
        public ActionResult NasiPolitici_GetList()
        {
            using (var db = new DbEntities())
            {
                string sql = @"
                    select distinct os.NameId, os.Jmeno, os.Prijmeni
                         , os.JmenoAscii, os.PrijmeniAscii, year(os.Narozeni) RokNarozeni, year(os.Umrti) RokUmrti
	                     , FIRST_VALUE(oes.organizace) OVER(partition by oes.osobaid order by oes.datumod desc) Aktpolstr
	                     , oec.pocet
                      from Osoba os
                      left join OsobaEvent oes on os.InternalId = oes.OsobaId and oes.AddInfo in (N'člen strany',N'předseda strany',N'místopředseda strany') and oes.Type = 7
                      left join (select COUNT(pk) pocet, OsobaId from OsobaEvent group by osobaid) oec on oec.OsobaId = os.InternalId
                     where os.Status = 3";

                var result = db.FindPersonView.FromSqlRaw(sql)
                    .Select(r => new
                    {
                        id = r.NameId,
                        name = r.Jmeno,
                        surname = r.Prijmeni,
                        asciiName = r.JmenoAscii,
                        asciiSurname = r.PrijmeniAscii,
                        birthYear = r.RokNarozeni,
                        deathYear = r.RokUmrti,
                        currentParty = r.Aktpolstr,
                        eventCount = r.Pocet
                    });

                string osoby = JsonConvert.SerializeObject(result);

                return Content(osoby, "application/json");
            }
        }

        [Authorize]
        public async Task<ActionResult> NasiPolitici_GetPoliticians()
        {
            var people = await OsobyEsRepo.YieldAllPoliticiansAsync().ToListAsync();
            string osoby = JsonConvert.SerializeObject(people);

            return Content(osoby, "application/json");
        }

        [Authorize]
        public async Task<ActionResult> NasiPolitici_GetData(string _id)
        {
            string id = _id;

            var o = OsobaRepo.GetByNameId(id);
            if (o == null)
            {
                Response.StatusCode = 404;
                return Json(ApiResponseStatus.Error(404, "Politik not found"));
            }
            if (o.StatusOsoby() != Osoba.StatusOsobyEnum.Politik)
            {
                Response.StatusCode = 404;
                return Json(ApiResponseStatus.Error(404, "Person is not marked as politician"));
            }

            var vazby = o.AktualniVazby(Relation.AktualnostType.Nedavny)
                .Where(v => v.Distance == 1 && v.To?.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .Take(10)
                .Select(v => new
                {
                    company = FirmaRepo.FromIco(v.To.Id).Jmeno,
                    ico = v.To.Id,
                    since = v.RelFrom,
                    until = v.RelTo,
                    description = v.Descr

                }).ToList();


            var statDescription =
                InfoFact.RenderInfoFacts((o.InfoFactsCached())
                    .Where(i => i.Level != InfoFact.ImportanceLevel.Stat).ToArray()
                    , 4, true, true, "", "{0}");

            var angazovanost =
                InfoFact.RenderInfoFacts((o.InfoFactsCached())
                    .Where(m => m.Level == InfoFact.ImportanceLevel.Stat).ToArray()
                    , 4, true, true, "", "{0}");


            int[] types = {
                (int)OsobaEvent.Types.VolenaFunkce,
                (int)OsobaEvent.Types.PolitickaPracovni,
                (int)OsobaEvent.Types.Politicka,
                (int)OsobaEvent.Types.VerejnaSpravaJine,
                (int)OsobaEvent.Types.VerejnaSpravaPracovni,
            };


            var roleOsoba = o.MergedEvents(m =>
                    types.Contains(m.Type)
                    && m.Status != (int)OsobaEvent.Statuses.NasiPoliticiSkryte)
                .Select(e => new
                {
                    role = e.AddInfo,
                    dateFrom = e.DatumOd,
                    dateTo = e.DatumDo,
                    organisation = e.Organizace
                })
                .ToArray();


            string osobaInsQuery = $"{{0}}.osobaId:{o.NameId}";
            //var oinsRes = Insolvence.SimpleSearch("osobaid:" + Model.NameId, 1, 5, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc, false, false);
            //query: dluznici.osobaId:{o.NameId}
            var oinsDluznik = await InsolvenceRepo.Searching.SimpleSearchAsync(string.Format(osobaInsQuery, "dluznici"), 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);
            //query: veritele.osobaId:{o.NameId}
            var oinsVeritel = await InsolvenceRepo.Searching.SimpleSearchAsync(string.Format(osobaInsQuery, "veritele"), 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);
            //query: spravci.await sobaId:{o.NameId}
            var oinsSpravce = await InsolvenceRepo.Searching.SimpleSearchAsync(string.Format(osobaInsQuery, "spravci"), 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);

            Dictionary<string, long> oinsolv = new Dictionary<string, long>();
            oinsolv.Add("dluznici|dlužník|dlužníka|dlužníkem", oinsDluznik.Total);
            oinsolv.Add("veritele|věřitel|věřitele|veřitelem", oinsVeritel.Total);
            oinsolv.Add("spravci|insolvenční správce|insolvenčního správce|insolvenčním správcem", oinsSpravce.Total);

            //var insRes = await InsolvenceRepo.Searching.SimpleSearchAsync("osobaid:" + o.NameId, 1, 5, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc, false, true);
            var insDluznik = await InsolvenceRepo.Searching.SimpleSearchAsync("osobaiddluznik:" + o.NameId, 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);
            var insVeritel = await InsolvenceRepo.Searching.SimpleSearchAsync("osobaidveritel:" + o.NameId, 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);
            var insSpravce = await InsolvenceRepo.Searching.SimpleSearchAsync("osobaidspravce:" + o.NameId, 1, 1, (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.FastestForScroll, false, true);

            Dictionary<string, long> insolv = new Dictionary<string, long>();
            insolv.Add("dluznik|dlužník|dlužníka|dlužníkem", insDluznik.Total);
            insolv.Add("veritel|věřitel|věřitele|veřitelem", insVeritel.Total);
            insolv.Add("spravce|insolvenční správce|insolvenčního správce|insolvenčním správcem", insSpravce.Total);

            var photo = o.GetPhotoUrl(false) + "?utm_source=nasipolitici&utm_medium=detail&utm_campaign=photo";

            var sponzorstvi = o.Sponzoring()
                .Select(m => new
                {
                    party = m.JmenoPrijemce(),
                    donatedAmount = m.Hodnota,
                    year = m.DarovanoDne?.Year,
                    source = m.Zdroj
                })
                .ToArray();

            var insPerson = new
            {
                debtorCount = oinsDluznik.Total,
                debtorLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=dluznici.osobaId:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=dluznikosoba",
                creditorCount = oinsVeritel.Total,
                creditorLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=veritele.osobaId:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=veritelosoba",
                bailiffCount = oinsSpravce.Total,
                bailiffLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=spravci.osobaId:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=spravceosoba",
            };

            var insCompany = new
            {
                debtorCount = insDluznik.Total,
                debtorLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=osobaiddluznik:{o.NameId}"
                                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=dluznikfirma",
                creditorCount = insVeritel.Total,
                creditorLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=osobaidveritel:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=veritelfirma",

                bailiffCount = insSpravce.Total,
                bailiffLink = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=osobaidspravce:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=spravcefirma",
            };

            var lastUpdate = o.Events(m => true)
                .OrderByDescending(e => e.Created)
                .Select(e => e.Created)
                .FirstOrDefault();


            var result = new
            {
                id = o.NameId,
                lastUpdate = lastUpdate,
                lastManualUpdate = o.ManuallyUpdated,
                namePrefix = o.TitulPred,
                nameSuffix = o.TitulPo,
                name = o.Jmeno,
                surname = o.Prijmeni,
                birthDate = o.Narozeni,
                deathDate = o.Umrti,
                status = o.StatusOsoby().ToString(),
                hasPhoto = o.HasPhoto(),
                photo = photo,
                description = statDescription,
                companyConnection = angazovanost,
                notificationRegisterId = "", //registrOznameni,   //byl smazán registr oznámení
                notificationRegisterStatements = Array.Empty<StatementDTO>(), //statements,
                //funkce = funkceOsoba,
                roles = roleOsoba,
                insolvencyPerson = insPerson,
                insolvencyCompany = insCompany,
                source = o.GetUrl(false),
                sponsor = sponzorstvi,
                currentParty = o.CurrentPoliticalParty(),
                contacts = o.GetSocialContacts(),
                connections = vazby,
                wikiId = o.WikiId,
                //sources
                sourceInsolvency = $"https://www.hlidacstatu.cz/insolvence/hledat?Q=osobaid:{o.NameId}"
                    + "&utm_source=nasipolitici&utm_medium=detail&utm_campaign=osoba",
                sourceSponzor = $"https://www.hlidacstatu.cz/osoba/{o.NameId}"
                                        + "?utm_source=nasipolitici&utm_medium=detail&utm_campaign=osoba",
                sourceRegisterStatements = "",
                //string.IsNullOrWhiteSpace(registrOznameni) 
                //? "https://www.hlidacstatu.cz" + "?utm_source=nasipolitici&utm_medium=detail&utm_campaign=osoba"
                //: $"https://www.hlidacstatu.cz/data/Detail/centralniregistroznameni/{registrOznameni}" + "?utm_source=nasipolitici&utm_medium=detail&utm_campaign=osoba",
                sourceRoles = $"https://www.hlidacstatu.cz/osoba/{o.NameId}" + "?utm_source=nasipolitici&utm_medium=detail&utm_campaign=osoba",
            };

            return Content(JsonConvert.SerializeObject(result), "application/json");
            // return Json(result);
        }
    }
}


