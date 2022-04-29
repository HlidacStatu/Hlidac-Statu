using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HlidacStatu.Lib.Data.External.Tables;

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


        [Authorize(Roles = "canEditData,TableEditor")]
        [HttpGet]
        public ActionResult ShowPrilohaTables(string s, string p)
        {
            Smlouva sml = SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            Lib.Data.External.Tables.Result[] res = SmlouvaPrilohaExtension.GetTablesFromPriloha(sml, pr);

            return View(res);
        }


        [Authorize(Roles = "canEditData,TableEditor")]
        [HttpGet]
        public ActionResult ShowPrilohaTablesOnePage(string s, string p, int page)
        {
            Smlouva sml = SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            if (page < 1)
                page = 1;

            return View(new Tuple<string, string, int>(s, p, page));
        }

        [Authorize(Roles = "canEditData,TableEditor")]
        [HttpGet]
        public ActionResult ShowPrilohaTablesOnePageImg(string s, string p, int page)
        {
            Smlouva sml = SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            string fn = Connectors.Init.PrilohaLocalCopy.GetFullPath(sml, pr);
            bool weHaveCopy = System.IO.File.Exists(fn);
            byte[] pdfBin = null;
            if (weHaveCopy)
                pdfBin = System.IO.File.ReadAllBytes(fn);
            else
            {
                using (var wc = new System.Net.WebClient())
                {
                    pdfBin = wc.DownloadData(pr.odkaz);
                }
            }
            if (pdfBin == null)
                return NotFound();

            //  prepare codec parameters

            using (var _img = PDFtoImage.Conversion.ToImage(pdfBin, page: page - 1))
            {
                using (var stream = new MemoryStream())
                {
                    _img.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return File(stream.ToArray(), "image/jpeg");
                }
            }
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
            Smlouva s = SmlouvaRepo.LoadAsync(id);
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


                await SmlouvaRepo.SaveAsync(s);
                return Redirect(s.GetUrl(true));
            }
        }

    }
}