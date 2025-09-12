using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task<ActionResult> ShowPrilohaTables(string s, string p)
        {
            Smlouva sml = await SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            HlidacStatu.Entities.DocTables res = SmlouvaPrilohaExtension.GetTablesInDocStructure(sml, pr);

            return View(res);
        }


        [Authorize(Roles = "canEditData,TableEditor")]
        [HttpGet]
        public async Task<ActionResult> ShowPrilohaTablesOnePage(string s, string p, int page)
        {
            Smlouva sml = await SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            if (page < 1)
                page = 1;

            return View(new Tuple<string, string, int>(s, p, page));
        }
        
        //adding just for test reasons
        [Authorize(Roles = "canEditData,TableEditor")]
        [HttpGet]
        public async Task<ActionResult> ShowPrilohaTablesOnePageImg(string s, string p, int page)
        {
            Smlouva sml = await SmlouvaRepo.LoadAsync(s);
            Smlouva.Priloha pr = sml?.Prilohy?.FirstOrDefault(m => m.hash.Value == p);
            if (sml == null || pr == null)
                return NotFound();

            string fn = SmlouvaRepo.GetDownloadedPrilohaPath(pr,sml, HlidacStatu.Connectors.IO.PrilohaFile.RequestedFileType.PDF);
            bool weHaveCopy = string.IsNullOrEmpty(fn)==false && System.IO.File.Exists(fn);
            byte[] pdfBin = null;
            if (weHaveCopy)
                pdfBin = await System.IO.File.ReadAllBytesAsync(fn);
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


            //  prepare codec parameters
            if (HlidacStatu.Util.FileMime.HasPDFHeaderFast(pdfBin))
            {
                using (var _img = PDFtoImage.Conversion.ToImage(pdfBin, page: page - 1))
                {
                    using (var stream = new MemoryStream())
                    {
                        _img.Encode(stream, SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
                        return File(stream.ToArray(), "image/jpeg");
                    }
                }
            }
            else
            {
                string errorsvg = "<svg xmlns=\"http://www.w3.org/2000/svg\" shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\" image-rendering=\"optimizeQuality\" fill-rule=\"evenodd\" clip-rule=\"evenodd\" viewBox=\"0 0 441 512.02\"><path d=\"M324.87 279.77c32.01 0 61.01 13.01 82.03 34.02 21.09 21 34.1 50.05 34.1 82.1 0 32.06-13.01 61.11-34.02 82.11l-1.32 1.22c-20.92 20.29-49.41 32.8-80.79 32.8-32.06 0-61.1-13.01-82.1-34.02-21.01-21-34.02-50.05-34.02-82.11s13.01-61.1 34.02-82.1c21-21.01 50.04-34.02 82.1-34.02zM243.11 38.08v54.18c.99 12.93 5.5 23.09 13.42 29.85 8.2 7.01 20.46 10.94 36.69 11.23l37.92-.04-88.03-95.22zm91.21 120.49-41.3-.04c-22.49-.35-40.21-6.4-52.9-17.24-13.23-11.31-20.68-27.35-22.19-47.23l-.11-1.74V25.29H62.87c-10.34 0-19.75 4.23-26.55 11.03-6.8 6.8-11.03 16.21-11.03 26.55v336.49c0 10.3 4.25 19.71 11.06 26.52 6.8 6.8 16.22 11.05 26.52 11.05h119.41c2.54 8.79 5.87 17.25 9.92 25.29H62.87c-17.28 0-33.02-7.08-44.41-18.46C7.08 432.37 0 416.64 0 399.36V62.87c0-17.26 7.08-32.98 18.45-44.36C29.89 7.08 45.61 0 62.87 0h173.88c4.11 0 7.76 1.96 10.07 5l109.39 118.34c2.24 2.43 3.34 5.49 3.34 8.55l.03 119.72c-8.18-1.97-16.62-3.25-25.26-3.79v-89.25zm-229.76 54.49c-6.98 0-12.64-5.66-12.64-12.64 0-6.99 5.66-12.65 12.64-12.65h150.49c6.98 0 12.65 5.66 12.65 12.65 0 6.98-5.67 12.64-12.65 12.64H104.56zm0 72.3c-6.98 0-12.64-5.66-12.64-12.65 0-6.98 5.66-12.64 12.64-12.64h142.52c3.71 0 7.05 1.6 9.37 4.15a149.03 149.03 0 0 0-30.54 21.14H104.56zm0 72.3c-6.98 0-12.64-5.66-12.64-12.65 0-6.98 5.66-12.64 12.64-12.64h86.2c-3.82 8.05-6.95 16.51-9.29 25.29h-76.91zm234.28 60.58h-28.01c-1.91-23.13-25.08-85.7 14.05-85.7 39.22 0 15.86 62.63 13.96 85.7zm-28.01 16.2h28.01v24.81h-28.01v-24.81z\"/></svg>";
                return Content(errorsvg, "image/svg+xml");
            }
        }

        [Authorize(Roles = "canEditData")]
        [HttpPost]
        public async Task<ActionResult> ZmenaSmluvnichStran(string id, IFormCollection form)
        {
            ViewBag.SmlouvaId = id;
            if (string.IsNullOrEmpty(form["platce"])
                || string.IsNullOrEmpty(form["prijemce"])
                )
            {
                ModelState.AddModelError("Check", "Nastav smluvni strany");
                return View();
            }
            Smlouva s = await SmlouvaRepo.LoadAsync(id, includePlaintext:false);
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
                    var iss = await ia.FindIssuesAsync(s);

                    if (iss != null)
                        issues.AddRange(iss);
                }

                s.Issues = issues.ToArray();


                await SmlouvaRepo.SaveAsync(s, fireOCRDone: false);
                return Redirect(s.GetUrl(true));
            }
        }

    }
}