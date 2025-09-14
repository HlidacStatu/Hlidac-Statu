using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Controllers
{
    [Authorize]
    public partial class ManageController : Controller
    {
        [Authorize(Roles = "canEditData")]
        public async Task<ActionResult> EditClassification(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var smlouva = await SmlouvaRepo.LoadAsync(id, includePlaintext:false);

            if (smlouva is null)
                return NotFound();

            return View(smlouva);
        }

        // set classification
        [Authorize(Roles = "canEditData")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditClassification(string id, string typ1, string typ2)
        {
            var smlouva = await SmlouvaRepo.LoadAsync(id, includePlaintext:false);
            if (smlouva is null)
                return NotFound();

            List<int> typeVals = new List<int>();
            if (int.TryParse(typ1, out int t1))
                typeVals.Add(t1);
            if (int.TryParse(typ2, out int t2))
                typeVals.Add(t2);


            if (typeVals.Count > 0)
            {
                await smlouva.OverrideClassificationAsync(typeVals.ToArray(), this.User.Identity.Name);
            }

            return Redirect(smlouva.GetUrl(true));
        }
    }
}