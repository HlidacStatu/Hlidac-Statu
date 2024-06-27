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

            HlidacStatu.Entities.PermanentLLM.FullSummary[] AIFulldocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.Entities.PermanentLLM.FullSummary>(
                HlidacStatu.Entities.PermanentLLM.FullSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.Entities.PermanentLLM.FullSummary.PARTTYPE
                );

            HlidacStatu.Entities.PermanentLLM.ShortSummary[] AIShortdocs = await HlidacStatu.Repositories.PermanentLLMRepo
                .SearchPerKeysAsync<HlidacStatu.Entities.PermanentLLM.ShortSummary>(
                HlidacStatu.Entities.PermanentLLM.ShortSummary.DOCUMENTTYPE,
                _id,
                HlidacStatu.Entities.PermanentLLM.ShortSummary.PARTTYPE
                );

            return View(new Tuple<Entities.PermanentLLM.ShortSummary[], Entities.PermanentLLM.FullSummary[], string>(
                AIShortdocs, AIFulldocs, modalId));
        }

    }
}


