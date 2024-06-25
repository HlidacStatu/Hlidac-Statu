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

            Entities.PermanentLLM.BaseItem<AI.LLM.PrivateLLM.SumarizaceJSON>[] AIdocs = await HlidacStatu.Repositories.PermanentLLMRepo<HlidacStatu.AI.LLM.PrivateLLM.SumarizaceJSON>
                .SearchPerKeysAsync(HlidacStatu.Entities.PermanentLLM.Summary.DOCUMENTTYPE, _id);


            return View(new Tuple<Entities.PermanentLLM.BaseItem<AI.LLM.PrivateLLM.SumarizaceJSON>[],string>(AIdocs, modalId));
        }

    }
}


