using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Insolvence;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Extensions;
using HlidacStatu.LibCore.Extensions;
using Review = HlidacStatu.Entities.Review;

namespace HlidacStatu.Web.Controllers
{
    [Authorize]
    public partial class ManageController : Controller
    {

        [AllowAnonymous]
        public ActionResult AddReview()
        {
            try
            {
                IDictionary<string, object> eo = new ExpandoObject() as IDictionary<string, object>;
                Review r = new Review();
                r.Created = DateTime.Now;
                r.CreatedBy = (User?.Identity?.Name ?? Request.Query["email"]) ?? "";
                r.NewValue = Request.Query["newV"].ToString();
                r.itemType = Request.Query["t"].ToString();
                string[] keys = new string[] { "t", "email", "oldV", "newV" };

                foreach (string key in Request.Query.Keys)
                {
                    if (key != null && !keys.Contains(key.ToLower()))
                        eo.Add(new KeyValuePair<string, object>(key, Request.Query[key]));
                }
                var oldValue = Request.Query["oldV"].ToString();

                dynamic data = eo;
                data.HtmlOldValue = oldValue;

                r.OldValue = Newtonsoft.Json.JsonConvert.SerializeObject(data);

                ReviewRepo.Save(r);

                return Json("1");

            }
            catch (Exception e)
            {
                _logger.Error(e, "addreview API error");
                return Json("0");
            }
        }

        public async Task<ActionResult> ExportResult(string id, string q, string h, string o, string ct, int? num = null, string ds = null)
        {
            try
            {

                var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("q", q),
                    new Framework.ApiCall.CallParameter("o", o),
                    new Framework.ApiCall.CallParameter("ct", ct),
                    new Framework.ApiCall.CallParameter("num", num?.ToString()),
                    new Framework.ApiCall.CallParameter("ds", ds)
                });

                if (!apiAuth.Authentificated)
                {
                    //Response.StatusCode = 401;
                    return Redirect("/");
                }

                int numOfRecords = num ?? 1000;
                if (string.IsNullOrEmpty(q) || (q?.Contains("*") == true && q?.Length < 5))
                    numOfRecords = 100;
                if (User.IsInRole("Admin") || User.IsInRole("novinar"))
                {
                    numOfRecords = 10000;
                }


                byte[] rawData = null;
                string contentType = "";
                string filename = "";
                List<dynamic> data = new List<dynamic>();


                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(q) || string.IsNullOrEmpty(h))
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                    return File(rawData, contentType, filename);
                }
                else if (SmlouvaRepo.Searching.IsQueryHashCorrect(id, q, h) == false) //TODO salt in config
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("nespravný požadavek");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                    return File(rawData, contentType, filename);
                }
                else if (id == "smlouvy")
                {
                    var sres = await SmlouvaRepo.Searching.SimpleSearchAsync(q, 0, numOfRecords, o, logError: false);

                    if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
                    {
                        Manager.LogQueryError<Smlouva>(sres.ElasticResults, "/hledej", HttpContext);
                        rawData = System.Text.Encoding.UTF8.GetBytes("chyba při přípravě dat. Omlouváme se a řešíme to");
                        contentType = "text/plain";
                        filename = "export.txt";
                        return File(rawData, contentType, filename);
                    }
                    foreach (var s in sres.Results)
                        data.Add(s.FlatExport());
                } //smlouvy
                else if (id == "zakazky")
                {

                    string[] cpvs = Request.Query["cpv"].ToString().Split(',');
                    string oblast = Request.Query["oblast"].ToString().Split(',').FirstOrDefault();
                    var sres = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(q, cpvs, 1, numOfRecords,
                        (Util.ParseTools.ToInt(o) ?? 0).ToString(), (Request.Query["zahajeny"] == "1"), 
                        oblast:oblast
                        );

                    if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("chyba při přípravě dat. Omlouváme se a řešíme to");
                        contentType = "text/plain";
                        filename = "export.txt";
                        return File(rawData, contentType, filename);
                    }
                    else
                    {
                        foreach (var s in sres.Results)
                            data.Add(s.FlatExport());
                    }
                }
                else if (id == "dataset")
                {
                    if (string.IsNullOrEmpty(ds))
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                        contentType = "text/plain";
                        filename = "chyba.txt";
                        return File(rawData, contentType, filename);
                    }

                    DataSet datasource = DataSet.CachedDatasets.Get(ds);
                    if (datasource == null)
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                        contentType = "text/plain";
                        filename = "chyba.txt";
                        return File(rawData, contentType, filename);
                    }
                    if (datasource.IsFlatStructure() == false)
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("Tato databáze nemá jednoduchou, plochou strukturu. Proto nemůže být exportována. Použijte API z hlidacstatu.cz/api");
                        contentType = "text/plain";
                        filename = "chyba.txt";
                        return File(rawData, contentType, filename);
                    }

                    DataSearchResult sres = await datasource.SearchDataAsync(q, 1, numOfRecords, (Util.ParseTools.ToInt(o) ?? 0).ToString());

                    if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("chyba při přípravě dat. Omlouváme se a řešíme to");
                        contentType = "text/plain";
                        filename = "export.txt";
                        return File(rawData, contentType, filename);
                    }
                    else
                    {
                        foreach (var s in sres.Result)
                        {
                            data.Add(datasource.ExportFlatObject(s));
                        }
                    }
                }
                else if (id == "dotace")
                {

                    var sres = await DotaceRepo.Searching.SimpleSearchAsync(q, 1, numOfRecords,
                        (Util.ParseTools.ToInt(o) ?? 0).ToString());

                    if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
                    {
                        rawData = System.Text.Encoding.UTF8.GetBytes("chyba při přípravě dat. Omlouváme se a řešíme to");
                        contentType = "text/plain";
                        filename = "export.txt";
                        return File(rawData, contentType, filename);
                    }
                    else
                    {
                        foreach (var s in sres.Results)
                            data.Add(s.FlatExport());
                    }
                }
                if (data.Count == 0)
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                }
                else
                {
                    if (ct == "tab")
                    {
                        rawData = new HlidacStatu.ExportData.TabDelimited().ExportData(new ExportData.Data(data));
                        contentType = "text/tab-separated-values";
                        filename = "export.tab";
                    }
                    else if (ct == "csv")
                    {
                        rawData = new HlidacStatu.ExportData.Csv().ExportData(new ExportData.Data(data));
                        contentType = "text/csv";
                        filename = "export.csv";
                    }
                    else if (ct == "numbers")
                    {
                        rawData = new HlidacStatu.ExportData.Excel().ExportData(new ExportData.Data(data));
                        contentType = "application/vnd.apple.numbers";
                        filename = "export.numbers";
                    }
                    else
                    {
                        rawData = new HlidacStatu.ExportData.Excel().ExportData(new ExportData.Data(data));
                        contentType = "application/vnd.ms-excel";
                        filename = "export.xlsx";

                    }

                }
                return File(rawData, contentType, filename);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Export error:  id={id}, q={q}, h={h}, o={o}, ct={ct}, num={num}, ds={ds}");
                return Content("Nastala chyba. Zkuste to pozdeji znovu", "text/plain");
            }

        }
        
        public async Task<ActionResult> FullExport(string q, string ds = null)
        {
            if(!User.IsInRole("Admin"))
                return new UnauthorizedResult();
            try
            {

                byte[] rawData = null;
                string contentType = "";
                string filename = "";
                List<dynamic> data = new List<dynamic>();
                
                if (string.IsNullOrEmpty(ds))
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                    return File(rawData, contentType, filename);
                }

                DataSet datasource = DataSet.CachedDatasets.Get(ds);
                if (datasource == null)
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                    return File(rawData, contentType, filename);
                }
                if (datasource.IsFlatStructure() == false)
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("Tato databáze nemá jednoduchou, plochou strukturu. Proto nemůže být exportována. Použijte API z hlidacstatu.cz/api");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                    return File(rawData, contentType, filename);
                }

                //var sres = datasource.SearchData(q, 1, 1000, (Util.ParseTools.ToInt(o) ?? 0).ToString());
                var res = (await datasource.GetAllDataForQueryAsync(q)).ToList();

                if (!res.Any())
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("chyba při přípravě dat. Omlouváme se a řešíme to");
                    contentType = "text/plain";
                    filename = "export.txt";
                    return File(rawData, contentType, filename);
                }
                else
                {
                    foreach (var s in res)
                    {
                        data.Add(datasource.ExportFlatObject(s));
                    }
                }
                
                if (data.Count == 0)
                {
                    rawData = System.Text.Encoding.UTF8.GetBytes("žádná data nejsou k dispozici");
                    contentType = "text/plain";
                    filename = "chyba.txt";
                }
                else
                {
                    rawData = new HlidacStatu.ExportData.TabDelimited().ExportData(new ExportData.Data(data));
                    contentType = "text/tab-separated-values";
                    filename = "export.tsv";
                }
                return File(rawData, contentType, filename);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Full export error:  q={q}, ds={ds}");
                return Content("Nastala chyba. Zkuste to pozdeji znovu", "text/plain");
            }

        }

        [Authorize(Roles = "canEditData")]
        public JsonResult RemovePhoto(string id, HlidacStatu.Entities.Osoba.PhotoTypes phototype)
        {
            var o = Osoby.GetByNameId.Get(id);
            if (o == null)
            {
                return new JsonResult(false);
            }
            if (o.HasPhoto())
            {
                var path = o.GetPhotoPath(phototype,true);
                if (System.IO.File.Exists(path))
                    Devmasters.IO.IOTools.DeleteFile(path, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(500), false);
            }
            return new JsonResult(true);
        }

        [Authorize(Roles = "canEditData")]
        public JsonResult DoPhotoRemoveBackground(string id)
        {

            var o = Osoby.GetByNameId.Get(id);
            if (o == null)
            {
                return new JsonResult(false);
            }
            if (o.HasPhoto())
            {
                var path = o.GetPhotoPath();
                if (System.IO.File.Exists(path))
                {
                    
                    var noBackGr = HlidacStatu.AI.Photo.RemoveBackgroundAsync(
                        new Uri(Devmasters.Config.GetWebConfigValue("RemoveBackgroundAPI")),
                        System.IO.File.ReadAllBytes(o.GetPhotoPath()), 
                        AI.Photo.RemoveBackgroundStyles.Person).Result;
                    if (noBackGr != null)
                    {
                        System.IO.File.WriteAllBytes(o.GetPhotoPath( Entities.Osoba.PhotoTypes.NoBackground,true), noBackGr);
                        return new JsonResult(true);
                    }
                    return new JsonResult(false);
                }
            }
            return new JsonResult(false);
        }
        public ActionResult RemoveBookmark(string id, int type)
        {
            try
            {
                BookmarkRepo.DeleteBookmark((Bookmark.ItemTypes)type, id, User.Identity.Name);
                return Json("0");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Manage.RemoveBookmark");
                return Json("-1");
            }
        }

        public ActionResult SetBookmark(string name, string url, string id, int type)
        {
            try
            {
                BookmarkRepo.SetBookmark(name, url, (Bookmark.ItemTypes)type, id, User.Identity.Name);
                return Json("1");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Manage.RemoveBookmark");
                return Json("-1");
            }
        }

        public ActionResult AddWd(string query, string dataType, string name, int period, int focus)
        {
            using (DbEntities db = new DbEntities())
            {

                if (string.IsNullOrEmpty(query))
                {
                    return Json("0");
                }
                string id = User.GetUserId();

                var dt = dataType;

                WatchDog wd = new WatchDog();
                wd.Created = DateTime.Now;
                wd.UserId = id;
                wd.StatusId = 1;
                wd.SearchTerm = query;
                wd.PeriodId = period;
                wd.FocusId = focus;
                wd.Name = Devmasters.TextUtil.ShortenText(name, 50);
                if (dt.ToLower() == typeof(Smlouva).Name.ToLower())
                    wd.DataType = typeof(Smlouva).Name;
                else if (dt.ToLower() == typeof(VerejnaZakazka).Name.ToLower())
                    wd.DataType = typeof(VerejnaZakazka).Name;
                else if (dt.ToLower() == typeof(Rizeni).Name.ToLower())
                    wd.DataType = typeof(Rizeni).Name;
                else if (dt.ToLower().StartsWith(typeof(DataSet).Name.ToLower()))
                {
                    var dataSetId = dt.Replace("DataSet.", "");
                    if (DataSet.ExistsDataset(dataSetId) == false)
                    {
                        _logger.Error("AddWd - try to hack, wrong dataType = " + dataType + "." + dataSetId);
                        throw new ArgumentOutOfRangeException("AddWd - try to hack, wrong dataType = " + dataType + "." + dataSetId);
                    }
                    wd.DataType = nameof(DataSet) + "." + dataSetId;
                }
                else if (dt == WatchDog.AllDbDataType)
                {
                    wd.DataType = dt;
                }
                else
                {
                    _logger.Error("AddWd - try to hack, wrong dataType = " + dataType);
                    throw new ArgumentOutOfRangeException("AddWd - try to hack, wrong dataType = " + dataType);
                }
                if (!db.WatchDogs
                     .Any(m => m.DataType == wd.DataType && m.UserId == id && m.SearchTerm == query))
                {
                    wd.Save();
                }

                return Json("1");
            }
        }


    }
}