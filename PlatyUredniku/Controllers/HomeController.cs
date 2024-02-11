using HlidacStatu.Entities.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using PlatyUredniku.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlatyUredniku.Controllers;

public class HomeController : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewData["platy"] = await PuRepo.GetPlatyAsync(PuRepo.DefaultYear);

        return View();
    }

    public async Task<IActionResult> DlePlatu(int id)
    {
        (int Min, int Max) range = id switch
        {
            2 => (20_000, 40_000),
            3 => (40_000, 70_000),
            4 => (70_000, 1000_000_000),
            _ => (0, 20_000)
        };


        var platy = await PuRepo.GetPoziceDlePlatuAsync(range.Min, range.Max, PuRepo.DefaultYear);

        ViewData["platy"] = platy;

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Action = nameof(DlePlatu),
                Name = $"Dle platu ({range.Min} - {range.Max})",
                Id = id.ToString()
            }
        };

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = $"Dle platů {range.Min} - {range.Max},-";

        return View(platy);
    }

    public async Task<IActionResult> Oblast(string oblast, string? podoblast = null)
    {
        var organizace = await PuRepo.GetOrganizaceForOblastiAsync(oblast, podoblast);

        ViewData["platy"] = organizace.SelectMany(o => o.Platy).ToList();

        ViewData["oblast"] = oblast;
        ViewData["podoblast"] = podoblast;

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = oblast,
                Action = nameof(Oblast),
                Id = oblast
            }
        };
        if (!string.IsNullOrWhiteSpace(podoblast))
            breadcrumbs.Add(new Breadcrumb()
            {
                Name = podoblast,
                Action = nameof(Oblast),
                Id = podoblast
            });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = $"{oblast} > {podoblast}";

        return View(organizace);
    }

    public async Task<IActionResult> Oblasti()
    {
        var oblasti = await PuRepo.GetPrimalOblasti();

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Action = nameof(Oblasti),
                Name = nameof(Oblasti),
                Id = ""
            }
        };

        ViewData["breadcrumbs"] = breadcrumbs;

        return View(oblasti);
    }

    public async Task<IActionResult> Detail2(int id, int rok = PuRepo.DefaultYear)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);
        ViewData["platy"] = detail.Platy.ToList();

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = detail.Oblast,
                Action = nameof(Oblast),
                Id = detail.Oblast,
            }
        };
        if (!string.IsNullOrWhiteSpace(detail.PodOblast))
            breadcrumbs.Add(new Breadcrumb()
            {
                Name = detail.PodOblast,
                Action = nameof(Oblast),
                Id = detail.PodOblast
            });

        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.Nazev,
            Action = nameof(Detail),
            Id = id.ToString(),
            Year = rok
        });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = detail.Nazev;

        return View(detail);
    }

    public async Task<IActionResult> Detail(int id, int? rok = null)
    {
        var detail = await PuRepo.GetDetailEagerAsync(id);

        ViewData["platy"] = detail.Platy.ToList();
        ViewData["rok"] = rok ?? (detail.Platy.Any() ? detail.Platy.Max(m => m.Rok) : PuRepo.DefaultYear);
        ViewData["id"] = id;

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = detail.Oblast,
                Action = nameof(Oblast),
                Id = detail.Oblast,
            }
        };
        if (!string.IsNullOrWhiteSpace(detail.PodOblast))
            breadcrumbs.Add(new Breadcrumb()
            {
                Name = detail.PodOblast,
                Action = nameof(Oblast),
                Id = detail.PodOblast
            });
        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.Nazev,
            Action = nameof(Detail2),
            Id = id.ToString(),
            Year = rok
        });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = detail.Nazev;

        return View(detail);
    }

    public async Task<IActionResult> Plat(int id)
    {
        var detail = await PuRepo.GetPlatAsync(id);

        List<Breadcrumb> breadcrumbs = new()
        {
            new Breadcrumb()
            {
                Name = detail.Organizace.Oblast,
                Action = nameof(Oblast),
                Id = detail.Organizace.Oblast,
            }
        };
        if (!string.IsNullOrWhiteSpace(detail.Organizace.PodOblast))
            breadcrumbs.Add(new Breadcrumb()
            {
                Name = detail.Organizace.PodOblast,
                Action = nameof(Oblast),
                Id = detail.Organizace.PodOblast
            });
        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.Organizace.Nazev,
            Action = nameof(Detail),
            Id = detail.Organizace.Id.ToString(),
            Year = detail.Rok
        });
        breadcrumbs.Add(new Breadcrumb()
        {
            Name = detail.NazevPozice,
            Action = nameof(Plat),
            Id = id.ToString(),
            Year = detail.Rok
        });

        ViewData["breadcrumbs"] = breadcrumbs;
        ViewData["context"] = $"{detail.NazevPozice} v organizaci {detail.Organizace.Nazev}";

        return View(detail);
    }
}