using HlidacStatu.Lib.Web.UI.Attributes;
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
            var model = await SponzoringRepo.PartiesPerYearsOverviewAsync(SponzoringRepo.DefaultLastSponzoringYear(), cancellationToken);
            model = model.Where(s => SponzoringRepo.TopStrany
                    .Any(x => string.Equals(x, s.KratkyNazev, StringComparison.InvariantCultureIgnoreCase)))
                .OrderByDescending(s => s.DaryCelkem)
                .ToList();
            return View(model);
        }

        public async Task<ActionResult> TopSponzori(int? rok, CancellationToken cancellationToken)
        {
            var model = await SponzoringRepo.BiggestPeopleSponsorsAsync(rok, cancellationToken);
            var filteredModel = model.Where(s => s.DarCelkem > 100000).ToList();
            ViewBag.Rok = rok ?? 0;
            var firstRow = filteredModel.FirstOrDefault();
            ViewBag.TopOsoba = OsobaRepo.GetByNameId(firstRow?.Id);
            ViewBag.TopOsobaAmount = firstRow?.DarCelkem ?? 0;

            return View(filteredModel);
        }

        [HlidacCache(12*3600, "rok", false)]
        public async Task<ActionResult> TopSponzoriFirmy(int? rok, CancellationToken cancellationToken)
        {
            System.Collections.Generic.List<Entities.Views.SponzoringSummed> model = 
                await SponzoringRepo.BiggestCompanySponsorsAsync(rok, cancellationToken);

            var filteredModel = model.Where(s => s.DarCelkem > 100000).ToList();
            ViewBag.Rok = rok ?? 0;
            var firstRow = filteredModel.FirstOrDefault();
            ViewBag.TopFirma = FirmaRepo.FromIco(firstRow?.Id);
            ViewBag.TopFirmaAmount = firstRow?.DarCelkem ?? 0;

            return View(filteredModel);
        }

        [HlidacCache(3600, "id;r", false)]
        public async Task<ActionResult> Strana(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();


            return View((object)id);
        }

        [HlidacCache(3600, "id;r", false)]
        public async Task<ActionResult> Strany(CancellationToken cancellationToken)
        {
            return View();
        }

        public async Task<ActionResult> OsobniSponzoriStrany(string id, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            ViewBag.Strana = id;
            var model = await SponzoringRepo.PeopleSponsorsAsync(id, cancellationToken);
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
            var model = await SponzoringRepo.CompanySponsorsAsync(id, cancellationToken);
            var firstRow = model.FirstOrDefault();
            ViewBag.TopFirma = FirmaRepo.FromIco(firstRow?.Id);
            ViewBag.TopFirmaAmount = firstRow?.DarCelkem ?? 0;

            return View(model);
        }


    }
}