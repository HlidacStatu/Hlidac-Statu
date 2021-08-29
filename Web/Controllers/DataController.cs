using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    public partial class DataController : Controller
    {

        static Devmasters.Cache.LocalMemory.LocalMemoryCache<Models.DatasetIndexStat[]> datasetIndexStatCache =
            new Devmasters.Cache.LocalMemory.LocalMemoryCache<Models.DatasetIndexStat[]>(TimeSpan.FromMinutes(15),
                (o) =>
                {
                    List<Models.DatasetIndexStat> ret = new List<Models.DatasetIndexStat>();
                    var datasets = DataSetDB.Instance.SearchDataRaw("*", 1, 200)
                        .Result
                        .Select(s => Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(s.Item2))
                        .Where(m => m.id != null);

                    foreach (var ds in datasets)
                    {
                        var rec = new Models.DatasetIndexStat() { Ds = ds };
                        var dsContent = DataSet.CachedDatasets.Get(ds.id.ToString());
                        var allrec = dsContent.SearchData("", 1, 1, sort: "DbCreated desc", exactNumOfResults: true);
                        rec.RecordNum = allrec.Total;
                        if (rec.RecordNum > 0)
                        {
                            rec.LastRecord = (DateTime?)allrec.Result.First().DbCreated;
                        }

                        var recordWeek = dsContent.SearchData($"DbCreated:[{DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")} TO *]", 1, 0, exactNumOfResults: true);
                        rec.RecordNumWeek = recordWeek.Total;
                        //string order = string.IsNullOrWhiteSpace(ds.defaultOrderBy) ? "DbCreated desc" : ds.defaultOrderBy;
                        //var data = dsContent.SearchDataRaw("*", 1, 1, order);

                        ret.Add(rec);
                    }
                    return ret.ToArray();
                }
                );




        public ActionResult Index(string id)
        {
            if (Request.Query["refresh"] == "1")
                datasetIndexStatCache.Invalidate();

            Models.DatasetIndexStat[] datasets = null;
            if (User.IsInRole("Admin") == true)
                datasets = datasetIndexStatCache.Get();
            else
                datasets = datasetIndexStatCache.Get().Where(m => m.Ds.hidden == false).ToArray();

            if (string.IsNullOrEmpty(id))
                return View(datasets);

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index", "Data", new { id = "" });

            return View("DatasetHomepage", ds);
        }

        public ActionResult TechnickeInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index");
            return View(ds);
        }

        public ActionResult Manage(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var ds = DataSet.CachedDatasets.Get(id);
                if (ds == null)
                    return Redirect("/data");

                if (ds.HasAdminAccess(Request?.HttpContext?.User))
                    return View(ds?.Registration());
            }
            return View();
        }

        public ActionResult Delete(string id, string confirmation)
        {
            string email = Request.HttpContext.User.Identity?.Name ?? "";
            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return Redirect("/data");
            if (ds.HasAdminAccess(Request?.HttpContext?.User) == false)
                return View("NoAccess");

            string[] neverDelete = new string[] { "veklep", "rozhodnuti-uohs", "centralniregistroznameni", "datasourcesdb" };
            if (neverDelete.Contains(ds.DatasetId.ToLower()))
                return View("NoAccess");

            if (confirmation == ds.DatasetId)
            {
                datasetIndexStatCache.Invalidate();

                DataSetDB.Instance.DeleteRegistration(ds.DatasetId, email);
                return RedirectToAction("Index");
            }
            return View(ds);
        }

        public ActionResult Backup(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index");

            if (ds.HasAdminAccess(Request?.HttpContext?.User) == false)
            {
                ViewBag.DatasetId = id;
                return View("NoAccess");
            }
            return File(
                System.Text.UTF8Encoding.UTF8.GetBytes(
                    Newtonsoft.Json.JsonConvert.SerializeObject(ds.Registration(), Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings() { ContractResolver = Serialization.PublicDatasetContractResolver.Instance })
                    ),
                "application/octet-streamSection", id + ".json");

        }




        public ActionResult Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index");

            if (ds.HasAdminAccess(Request?.HttpContext?.User) == false)
            {
                ViewBag.DatasetId = id;
                return View("NoAccess");
            }

            return View(ds.Registration());
        }

        [HttpPost]
        public ActionResult Edit(string id, Registration update, IFormCollection form)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index");

            datasetIndexStatCache.Invalidate();

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index");


            if (ds.HasAdminAccess(Request?.HttpContext?.User) == false)
            {
                ViewBag.DatasetId = id;
                return View("NoAccess");
            }

            //
            var newReg = Newtonsoft.Json.JsonConvert.DeserializeObject<Registration>(
                Newtonsoft.Json.JsonConvert.SerializeObject(update, DataSet.DefaultDeserializationSettings)
                , DataSet.DefaultDeserializationSettings);
            if (newReg.datasetId != id)
            {
                ViewBag.DatasetId = id;
                return View("NoAccess");
            }

            newReg = WebFormToRegistration(newReg, form);
            if (string.IsNullOrEmpty(newReg.createdBy))
                newReg.createdBy = Request?.HttpContext?.User?.Identity?.Name;


            var res = DataSet.Api.Update(newReg, ApplicationUser.GetByEmail(Request?.HttpContext?.User?.Identity?.Name));
            if (res.valid)
                return RedirectToAction("Edit", "Data", new { id = ds.DatasetId });
            else
            {
                ViewBag.ApiResponseError = res;
                return View(newReg);
            }
        }

        private Registration WebFormToRegistration(Registration newReg, IFormCollection form)
        {

            if (!string.IsNullOrEmpty(form["searchResultTemplate_body"].ToString().Trim()))
            {
                if (newReg.searchResultTemplate == null)
                    newReg.searchResultTemplate = new Registration.Template();
                newReg.searchResultTemplate.body = form["searchResultTemplate_body"];
            }
            if (!string.IsNullOrEmpty(form["detailTemplate_body"].ToString().Trim()))
            {
                if (newReg.detailTemplate == null)
                    newReg.detailTemplate = new Registration.Template();
                newReg.detailTemplate.body = form["detailTemplate_body"];
            }
            string[] orderlines = form["sorderList"].ToString()
                ?.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries)
                ?.Where(m => !string.IsNullOrEmpty(m.Trim()))
                ?.ToArray() ?? new string[] { };

            string[,] orderList = null;
            if (orderlines.Count() > 0)
            {
                orderList = new string[orderlines.Count(), 2];
                for (int i = 0; i < orderlines.Count(); i++)
                {
                    var oo = orderlines[i].Split('|');
                    if (oo.Length == 2)
                    {
                        orderList[i, 0] = oo[0].Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                        orderList[i, 1] = oo[1].Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
                    }
                }
            }
            else
                orderList = new string[,] { { Registration.DbCreatedLabel, "DbCreated" } };

            newReg.orderList = orderList;

            return newReg;
        }

        [HttpPost]
        public ActionResult DatasetTextJson(string id, IFormCollection form)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("index");

            var ds = DataSet.CachedDatasets.Get(id);
            if (ds == null)
                return RedirectToAction("index");

            ViewBag.jsondata = form["jsondata"].ToString();
            return View("DatasetTextJson", ds);
        }

        public ActionResult Napoveda(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("index");


            DataSet datasource = null;
            datasource = DataSet.CachedDatasets.Get(id);
            if (datasource == null)
                return RedirectToAction("index", new { id = id });

            return View(datasource);
        }
        public ActionResult Hledat(string id, DataSearchRawResult model)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("index");


            DataSet ds = null;
            try
            {
                ds = DataSet.CachedDatasets.Get(id);
                if (ds == null)
                    return RedirectToAction("index");

                if (ds.Registration().hidden == true && (User.Identity?.IsAuthenticated == false || User.IsInRole("Admin") == false))
                    return RedirectToAction("index");

                model = ds.SearchDataRaw(model.Q, model.Page, model.PageSize, model.Order);
                AuditRepo.Add(
                    Audit.Operations.UserSearch
                    , User?.Identity?.Name
                    , HttpContext.Connection.RemoteIpAddress?.ToString()
                    , "Dataset." + ds.DatasetId
                    , model.IsValid ? "valid" : "invalid"
                    , model.Q, model.OrigQuery);

                return View(model);
            }
            catch (DataSetException e)
            {
                if (e.APIResponse.error.number == ApiResponseStatus.InvalidSearchQuery.error.number)
                {
                    model.DataSet = ds;
                    model.IsValid = false;
                    return View(model);
                }
                return RedirectToAction("index", new { id = id });

            }
            catch (Exception)
            {
                return RedirectToAction("index", new { id = id });
            }

        }

        public ActionResult Detail(string id, string dataid)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("index");

            try
            {
                var ds = DataSet.CachedDatasets.Get(id);
                if (ds == null)
                    return RedirectToAction("index");
                if (string.IsNullOrEmpty(dataid))
                    return RedirectToAction("index", new { id = id });

                if (ds.Registration().hidden == true && (User.Identity?.IsAuthenticated == false || User.IsInRole("Admin") == false))
                    return RedirectToAction("index");

                var dataItem = ds.GetData(dataid);
                if (dataItem == null)
                    return RedirectToAction("index", new { id = id });

                if (!string.IsNullOrEmpty(this.Request.Query["qs"]))
                {
                    try
                    {
                        var findSm = ds.SearchDataRaw($"_id:\"{dataid}\" AND ({this.Request.Query["qs"]})", 1, 1,
                            null, withHighlighting: true);
                        if (findSm.Total > 0)
                            ViewBag.Highlighting = findSm.ElasticResultsRaw.Hits.First().Highlight;

                    }
                    catch (Exception e)
                    {

                        Util.Consts.Logger.Error("Dataset Detail Highligting query error ", e);
                    }

                }

                ViewBag.Id = id;
                return View(new Models.DataDetailModel() { Dataset = ds, Data = dataid });
            }
            catch (DataSetException ex)
            {
                Util.Consts.Logger.Error("Dataset Detail", ex);
                return RedirectToAction("index");
            }

        }

        public ActionResult DetailText(string id, string dataid)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("index");

            try
            {
                var ds = DataSet.CachedDatasets.Get(id);
                if (ds == null)
                    return RedirectToAction("index");

                if (ds.Registration().hidden == true && (User.Identity?.IsAuthenticated == false || User.IsInRole("Admin") == false))
                    return RedirectToAction("index");

                if (string.IsNullOrEmpty(dataid))
                    return RedirectToAction("index", new { id = id });

                var dataItem = ds.GetData(dataid);
                if (dataItem == null)
                    return RedirectToAction("index", new { id = id });

                ViewBag.Id = id;
                return View(new Models.DataDetailModel() { Dataset = ds, Data = dataid });
            }
            catch (DataSetException)
            {
                return RedirectToAction("index");
            }

        }

    }
}
