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

            HlidacStatu.AI.LLM.Entities.FullSummary[] AIFulldocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.AI.LLM.Entities.FullSummary>(
                HlidacStatu.AI.LLM.Entities.FullSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.AI.LLM.Entities.FullSummary.PARTTYPE
                );

            HlidacStatu.AI.LLM.Entities.ShortSummary[] AIShortdocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.AI.LLM.Entities.ShortSummary>(
                HlidacStatu.AI.LLM.Entities.ShortSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.AI.LLM.Entities.ShortSummary.PARTTYPE
                );

            return View(new Tuple<HlidacStatu.AI.LLM.Entities.ShortSummary[], HlidacStatu.AI.LLM.Entities.FullSummary[], string>(
                AIShortdocs, AIFulldocs, modalId));
        }

    }
}


