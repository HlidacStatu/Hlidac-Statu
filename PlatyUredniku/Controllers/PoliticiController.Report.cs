using HlidacStatu.Entities;
using HlidacStatu.ExportData;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schema.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;
using static HlidacStatu.Repositories.PpRepo;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin")]
public partial class PoliticiController : Controller
{


    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> Reporty()
    {
        return View();
    }


    [HlidacCache(60 * 60, "*")]
    [Route("Politici/Report/{id}")]
    public async Task<IActionResult> Report(string id)
    {

        switch (id)
        {
            case "top-prijem":
                return View("List", (Group: PpRepo.PoliticianGroup.Vse, Year: PpRepo.DefaultYear, top: 100, report: id));
            case "top-funkce":
                return View("List", (Group: PpRepo.PoliticianGroup.Vse, Year: PpRepo.DefaultYear, top: 100, report: id));
            default:
                return View("report_" + id);
        }

    }


}