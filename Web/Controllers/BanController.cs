using System;
using System.Globalization;
using System.Threading.Tasks;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BanController : Controller
    {
        // GET
        public IActionResult Index()
        {
            var bannedIps = BannedIpRepoCached.GetBannedIps();
            return View(bannedIps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanIp(string ipAddress, string expiration, int lastStatusCode, string pathList)
        {

            DateTime.TryParseExact(expiration, "d.M.yyyy", CultureInfo.CurrentCulture,
                DateTimeStyles.AllowWhiteSpaces, out var expirationDate);

            await BannedIpRepoCached.BanIp(ipAddress, expirationDate, lastStatusCode, pathList);

            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AllowIp(string ipAddress)
        {
            await BannedIpRepoCached.AllowIp(ipAddress);

            return View();
        }
    }
}