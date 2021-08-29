using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Controllers
{
    public class SponzoriController : Controller
    {

        [HlidacCache(3600, "", false)]
        public async Task<ActionResult> Index(CancellationToken cancellationToken)
        {
            var model = await SponzoringRepo.PartiesPerYearsOverview(SponzoringRepo.DefaultLastSponzoringYear, cancellationToken);
            model = model.Where(s => SponzoringRepo.TopStrany
                    .Any(x => string.Equals(x, s.KratkyNazev, StringComparison.InvariantCultureIgnoreCase)))
                .OrderByDescending(s => s.DaryCelkem)
                .ToList();
            return View(model);
        }

        public async Task<ActionResult> TopSponzori(int? rok, CancellationToken cancellationToken)
        {
            var model = await SponzoringRepo.BiggestPeopleSponsors(rok, cancellationToken);
            var filteredModel = model.Where(s => s.DarCelkem > 100000).ToList();
            ViewBag.Rok = rok ?? 0;
            var firstRow = filteredModel.FirstOrDefault();
            ViewBag.TopOsoba = OsobaRepo.GetByNameId(firstRow?.Id);
            ViewBag.TopOsobaAmount = firstRow?.DarCelkem ?? 0;

            return View(filteredModel);
        }

        public async Task<ActionResult> TopSponzoriFirmy(int? rok, CancellationToken cancellationToken)
        {
            var model = await SponzoringRepo.BiggestCompanySponsors(rok, cancellationToken);
            var filteredModel = model.Where(s => s.DarCelkem > 100000).ToList();
            ViewBag.Rok = rok ?? 0;
            var firstRow = filteredModel.FirstOrDefault();
            ViewBag.TopFirma = FirmaRepo.FromIco(firstRow?.Id);
            ViewBag.TopFirmaAmount = firstRow?.DarCelkem ?? 0;

            return View(filteredModel);
        }


        public async Task<ActionResult> SponzoriStrany(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            ViewBag.Strana = id;
            var model = await SponzoringRepo.PeopleSponsors(id, cancellationToken);
            var firstRow = model.FirstOrDefault();
            ViewBag.TopOsoba = OsobaRepo.GetByNameId(firstRow?.Id);
            ViewBag.TopOsobaAmount = firstRow?.DarCelkem ?? 0;

            return View(model);
        }

        public async Task<ActionResult> FiremniSponzoriStrany(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            ViewBag.Strana = id;
            var model = await SponzoringRepo.CompanySponsors(id, cancellationToken);
            var firstRow = model.FirstOrDefault();
            ViewBag.TopFirma = FirmaRepo.FromIco(firstRow?.Id);
            ViewBag.TopFirmaAmount = firstRow?.DarCelkem ?? 0;

            return View(model);
        }


    }
}