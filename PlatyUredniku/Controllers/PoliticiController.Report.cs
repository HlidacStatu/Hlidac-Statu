using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace PlatyUredniku.Controllers;

[Authorize(Roles = "Admin")]
public partial class PoliticiController
{


    [HlidacCache(60 * 60, "rok")]
    public async Task<IActionResult> Reporty()
    {
        return View();
    }


    [HlidacCache(60 * 60, "*")]
    [Route("Politici/Reporty/{id}")]
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