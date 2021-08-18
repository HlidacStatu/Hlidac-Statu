using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    [Authorize]
    public partial class ManageController : Controller
    {

        [Authorize(Roles = "canEditData")]
        [HttpGet]
        public ActionResult ZmenaSmluvnichStran(string id)
        {
            ViewBag.SmlouvaId = id;
            return View();
        }


        [Authorize(Roles = "canEditData")]
        [HttpGet]
        public ActionResult ShowPrilohaTables(string s, string p)
        {
            Smlouva sml = SmlouvaRepo.Load(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            Lib.Data.External.Camelot.CamelotResult[] res = Extensions.SmlouvaPrilohaExtension.GetTablesFromPriloha(sml, pr);

            return View(res);
        }

        [Authorize(Roles = "canEditData")]
        [HttpPost]
        public ActionResult ZmenaSmluvnichStran(string id, IFormCollection form)
        {
            ViewBag.SmlouvaId = id;
            if (string.IsNullOrEmpty(form["platce"]) 
                || string.IsNullOrEmpty(form["prijemce"])
                )
                {
                ModelState.AddModelError("Check", "Nastav smluvni strany");
                return View();
            }
            Smlouva s = SmlouvaRepo.Load(id);
            if (s == null)
            {
                ModelState.AddModelError("Check", "smlouva neexistuje");
                return View();
            }
            else
            {
                Plugin.Enhancers.ManualChanges mch = new();

                var allSubjList = s.Prijemce.ToList();
                allSubjList.Insert(0, s.Platce);

                var platce = allSubjList[Convert.ToInt32(form["platce"])];
                List<Smlouva.Subjekt> prijemci = new List<Smlouva.Subjekt>();
                var iprijemci = form["prijemce"].ToString().Split(',').Select(m => Convert.ToInt32(m));
                foreach (var i in iprijemci)
                {
                    prijemci.Add(allSubjList[i]);
                }

                mch.UpdateSmluvniStrany(ref s, platce, prijemci.ToArray());

                List<Entities.Issues.Issue> issues = new List<Entities.Issues.Issue>();
                foreach (var ia in Entities.Issues.Util.GetIssueAnalyzers())
                {
                    var iss = ia.FindIssues(s);

                    if (iss != null)
                        issues.AddRange(iss);
                }

                s.Issues = issues.ToArray();


                SmlouvaRepo.Save(s);
                return Redirect(s.GetUrl(true));
            }
        }

    }
}