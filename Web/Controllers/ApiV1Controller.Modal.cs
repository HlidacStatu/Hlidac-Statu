using Microsoft.AspNetCore.Mvc;

using System;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Controllers
{
    public partial class ApiV1Controller : Controller
    {
        public ActionResult ModalClassification(string _id, string modalId)
        {
            string id = _id;

            return View(new Tuple<string, string>(id, modalId));
        }

        public ActionResult ModalZneplatnenaSmlouva(string _id, string modalId)
        {
            string id = _id;

            return View(new Tuple<string, string>(id, modalId));
        }

        public async Task<ActionResult> ModalAISummary(string _id, string modalId)
        {
            string id = _id;

            HlidacStatu.Entities.AI.FullSummary[] AIFulldocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.Entities.AI.FullSummary>(
                HlidacStatu.Entities.AI.FullSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.Entities.AI.FullSummary.PARTTYPE
                );

            HlidacStatu.Entities.AI.ShortSummary[] AIShortdocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.Entities.AI.ShortSummary>(
                HlidacStatu.Entities.AI.ShortSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.Entities.AI.ShortSummary.PARTTYPE
                );

            return View(new Tuple<HlidacStatu.Entities.AI.ShortSummary[], HlidacStatu.Entities.AI.FullSummary[], string>(
                AIShortdocs, AIFulldocs, modalId));
        }

    }
}


