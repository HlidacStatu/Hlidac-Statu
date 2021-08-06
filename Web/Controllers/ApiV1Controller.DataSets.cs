using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using HlidacStatu.Datasets;
using HlidacStatu.Extensions;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{
    public partial class ApiV1Controller : Controller
    {
        [NonAction]
        public ActionResult _templateAction(string query, int? page, int? order)
        {
            page = page ?? 1;
            order = order ?? 0;

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("query", query),
                    new Framework.ApiCall.CallParameter("page", page?.ToString()),
                    new Framework.ApiCall.CallParameter("order", order?.ToString())
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                return Json(new { Ok = true });
            }
        }

        public ActionResult GetJmenoPrijmeniFromString(string text)
        {
            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                   parameters: new Framework.ApiCall.CallParameter[] {
                                new Framework.ApiCall.CallParameter("text", text),
                   })
                   .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                var o = Validators.JmenoInText(text);
                if (o == null)
                    return Json(new { });
                else
                    return Json(new
                    {
                        titulPred = o.TitulPred,
                        jmeno = o.Jmeno,
                        prijmeni = o.Prijmeni,
                        titulPo = o.TitulPo
                    });
            }

        }

        public ActionResult FindOsobaId(string jmeno, string prijmeni, string celejmeno, string narozeni, string funkce)
        {

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("jmeno", jmeno),
                    new Framework.ApiCall.CallParameter("prijmeni", prijmeni),
                    new Framework.ApiCall.CallParameter("celejmeno", celejmeno),
                    new Framework.ApiCall.CallParameter("narozeni", narozeni),
                    new Framework.ApiCall.CallParameter("funkce", funkce)
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                DateTime? dt = Devmasters.DT.Util.ToDateTime(narozeni, "yyyy-MM-dd");
                if (dt.HasValue == false && string.IsNullOrEmpty(funkce))
                {
                    var status = ApiResponseStatus.InvalidFormat;
                    status.error.errorDetail = "invalid date format for parameter 'narozeni'. Use yyyy-MM-dd format.";
                    return Json(status);
                }

                if (string.IsNullOrEmpty(jmeno) && string.IsNullOrEmpty(prijmeni) && !string.IsNullOrEmpty(celejmeno))
                {
                    Osoba osobaZeJmena = Validators.JmenoInText(celejmeno);
                    if (osobaZeJmena == null)
                    {
                        jmeno = "";
                        prijmeni = "";
                    }
                    else
                    {
                        jmeno = osobaZeJmena.Jmeno;
                        prijmeni = osobaZeJmena.Prijmeni;
                    }
                }
                if (string.IsNullOrEmpty(jmeno) || string.IsNullOrEmpty(prijmeni))
                {
                    var status = ApiResponseStatus.InvalidFormat;
                    status.error.errorDetail = "no data for parameter 'jmeno' or 'prijmeni' or 'celejmeno'.";
                    return Json(status);

                }

                IEnumerable<Osoba>? found;
                if (dt.HasValue)
                    found = FindByDate(jmeno, prijmeni, dt);
                else
                    found = FindByFunkce(jmeno, prijmeni, funkce);

                if (found == null || found?.Count() == 0)
                    return Json(new { });
                else
                {
                    var f = found.First();
                    return Json(new
                    {
                        Jmeno = f.Jmeno,
                        Prijmeni = f.Prijmeni,
                        //Narozeni = found.Narozeni.Value.ToString("yyyy-MM-dd"),
                        OsobaId = f.NameId
                    }
                            );
                }
            }
        }

        [NonAction]
        private IEnumerable<Osoba>? FindByFunkce(string jmeno, string prijmeni, string funkce)
        {
            if (string.IsNullOrEmpty(funkce))
            {
                return new Osoba[] { };
            }

            var found = OsobaRepo.PolitickyAktivni.Get()
                        .Where(o =>
                            string.Equals(o.Jmeno, jmeno, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(o.Prijmeni, prijmeni, StringComparison.OrdinalIgnoreCase)
                            )
                        .ToArray();
            ;

            if (found?.Count() > 0)
            {

            }
            else
            {
                string jmenoasc = Devmasters.TextUtil.RemoveDiacritics(jmeno);
                string prijmeniasc = Devmasters.TextUtil.RemoveDiacritics(prijmeni);
                found = OsobaRepo.PolitickyAktivni.Get()
                            .Where(o =>
                                string.Equals(o.JmenoAscii, jmenoasc, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(o.PrijmeniAscii, prijmeniasc, StringComparison.OrdinalIgnoreCase)
                                )
                            .ToArray();
            }

            funkce = Util.ParseTools.NormalizePolitikFunkce(funkce);

            found = found
                .Where(m =>
                    m.Events().Any(e => Util.ParseTools.FindInStringSqlLike(e.AddInfo, funkce))
                    )
                .ToArray();

            return found ?? new Osoba[] { };
        }


        [NonAction]
        private IEnumerable<Osoba>? FindByDate(string jmeno, string prijmeni, DateTime? dt)
        {
            if (dt.HasValue == false)
            {
                return new Osoba[] { };
            }

            var found = OsobaRepo.PolitickyAktivni.Get()
                        .Where(o =>
                            string.Equals(o.Jmeno, jmeno, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(o.Prijmeni, prijmeni, StringComparison.OrdinalIgnoreCase)
                            && o.Narozeni == dt.Value);

            if (found?.Count() > 0)
                return found;

            string jmenoasc = Devmasters.TextUtil.RemoveDiacritics(jmeno);
            string prijmeniasc = Devmasters.TextUtil.RemoveDiacritics(prijmeni);
            found = OsobaRepo.PolitickyAktivni.Get()
                        .Where(o =>
                            string.Equals(o.JmenoAscii, jmenoasc, StringComparison.OrdinalIgnoreCase)
                            && string.Equals(o.PrijmeniAscii, prijmeniasc, StringComparison.OrdinalIgnoreCase)
                            && o.Narozeni == dt.Value)
                        ;

            return found ?? new Osoba[] { };
        }

        
        [HttpGet]
        public ActionResult FindCompanyID(string companyName)
        {
            return CompanyID(companyName);
        }
        [HttpGet]
        public ActionResult CompanyID(string companyName)
        {
            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("companyName", companyName)
                })
                .Authentificated)
            {
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(companyName))
                        return Json(new { });
                    else
                    {
                        //Firma f = Validators.FirmaInText(companyName);
                        var name = Firma.JmenoBezKoncovky(companyName);
                        var found = FirmaRepo.Searching.FindAll(name, 1).FirstOrDefault();
                        if (found == null)
                            return Json(new { });
                        else
                            return Json(new { ICO = found.ICO, Jmeno = found.Jmeno, DatovaSchranka = found.DatovaSchranka });
                    }


                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Error("Dataset API", ex);
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));
                }

            }
        }

        [HttpPost, ActionName("Datasets")]
        public ActionResult Datasets_Create()
        {
            var data = ReadRequestBody(HttpContext.Request);
            var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("data", data)
                });
            if (!apiAuth.Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    var reg = JsonConvert.DeserializeObject<Registration>(data, DataSet.DefaultDeserializationSettings);
                    var res = DataSet.Api.Create(reg, apiAuth.ApiCall.User);

                    if (res.valid)
                        return Json(new { datasetId = ((DataSet)res.value).DatasetId });
                    else
                        return Json(res);
                }
                catch (JsonSerializationException jex)
                {
                    var status = ApiResponseStatus.DatasetItemInvalidFormat;
                    status.error.errorDetail = jex.Message;
                    return Json(status);
                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Error("Dataset API", ex);
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));
                }
            }
        }


        [HttpPut, ActionName("Datasets")]
        public ActionResult Datasets_Update(string _id, [FromBody]Registration data)
        {
            string id = _id;

            var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id)
                });
            if (!apiAuth.Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }

            if (ModelState.IsValid)
                return Json(DataSet.Api.Update(data, ApplicationUser.GetByEmail(apiAuth.ApiCall.User))); //blablablabla apiAuth.ApiCall?.User));

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            var errorsStringified = string.Join(";\n", errors);
            Util.Consts.Logger.Error($"Dataset API:\n {errorsStringified}"); 
            return Json(ApiResponseStatus.GeneralExceptionError(errorsStringified));

        }

        [HttpPut, ActionName("DatasetsPart")]
        public ActionResult DatasetsPart_Update(string _id, string atribut, [FromBody]Registration data)
        {
            string id = _id;

            if (string.IsNullOrEmpty(atribut))
                return Json(ApiResponseStatus.InvalidFormat);

            var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("atribut", atribut)
                });
            if (!apiAuth.Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    return Json(DataSet.Api.Update(data,
                        ApplicationUser.GetByEmail(apiAuth.ApiCall.User))); //blablablabla apiAuth.ApiCall?.User?.ToLower()));
                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
            }
            
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            var errorsStringified = string.Join(";\n", errors);
            Util.Consts.Logger.Error($"Dataset API:\n {errorsStringified}"); 
            return Json(ApiResponseStatus.GeneralExceptionError(errorsStringified));
        }


        [HttpPatch, ActionName("Datasets")]
        public ActionResult Datasets_Patch(string _id)
        {
            string id = _id;

            return Json(ApiResponseStatus.ApiUnauthorizedAccess);
        }
        [HttpOptions, ActionName("Datasets")]
        public ActionResult Datasets_Options(string _id)
        {
            string id = _id;

            return Json(ApiResponseStatus.ApiUnauthorizedAccess);
        }
        [HttpGet, ActionName("Datasets")]
        public ActionResult Datasets_GET(string _id)
        {
            string id = _id;

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id)
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                        return Content(JsonConvert.SerializeObject(DataSetDB.Instance.SearchData("*", 1, 100).Result,
                                Formatting.None,
                                new JsonSerializerSettings() { ContractResolver = Serialization.PublicDatasetContractResolver.Instance })
                            , "application/json");
                    else
                    {
                        var ds = DataSet.CachedDatasets.Get(id);
                        if (ds == null)
                            return Json(ApiResponseStatus.DatasetNotFound);
                        else
                            return Content(JsonConvert.SerializeObject(ds.Registration()), "application/json");
                    }

                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Error("Dataset API", ex);
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));

                }

            }
        }

        [HttpDelete, ActionName("Datasets")]
        public ActionResult Datasets_Delete(string _id)
        {
            string id = _id;

            var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id)
                });
            if (!apiAuth.Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    if (string.IsNullOrEmpty(id))
                        return Json(ApiResponseStatus.DatasetNotFound);

                    id = id.ToLower();
                    var r = DataSetDB.Instance.GetRegistration(id);
                    if (r == null)
                        return Json(ApiResponseStatus.DatasetNotFound);

                    if (r.createdBy != null && apiAuth.ApiCall.User.ToLower() != r.createdBy?.ToLower())
                    {
                        return Json(ApiResponseStatus.DatasetNoPermision);
                    }

                    var res = DataSetDB.Instance.DeleteRegistration(id, apiAuth.ApiCall.User);
                    return Json(new ApiResponseStatus() { valid = res });

                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Error("Dataset API", ex);
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));

                }

            }
        }


        [HttpGet, ActionName("DatasetSearch")]
        public ActionResult DatasetSearch(string _id, string q, int? page, string sort = null, string desc = "0")
        {
            string id = _id;

            page = page ?? 1;
            if (page < 1)
                page = 1;
            if (page > 200)
                return Content(
                    JsonConvert.SerializeObject(
                    new { total = 0, page = 201, results = Array.Empty<dynamic>() }
                )
                , "application/json");

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("q", q),
                    new Framework.ApiCall.CallParameter("page", page?.ToString()),
                    new Framework.ApiCall.CallParameter("sort", sort)
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    var ds = DataSet.CachedDatasets.Get(id?.ToLower());
                    if (ds == null)
                        return Json(ApiResponseStatus.DatasetNotFound);

                    
                    bool bDesc = (desc == "1" || desc?.ToLower() == "true");
                    var res = ds.SearchData(q, page.Value, 50, sort + (bDesc ? " desc" : ""));
                    res.Result = res.Result.Select(m => { m.DbCreatedBy = null; return m; });


                    return Content(
                        JsonConvert.SerializeObject(
                        new { total = res.Total, page = res.Page, results = res.Result }
                    )
                    , "application/json");

                    

                }
                catch (DataSetException dex)
                {
                    return Json(dex.APIResponse);
                }
                catch (Exception ex)
                {
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));
                }

            }
        }

        [HttpGet, ActionName("DatasetItem")]
        public ActionResult DatasetItem_Get(string _id, string _dataid)
        {
            string id = _id;
            string dataid = _dataid;

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("dataid", dataid)
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    var ds = DataSet.CachedDatasets.Get(id.ToLower());
                    var value = ds.GetDataObj(dataid);
                    //remove from item
                    if (value == null)
                    {
                        return Content("null", "application/json");
                    }
                    else
                    {
                        value.DbCreatedBy = null;
                        return Content(
                            JsonConvert.SerializeObject(
                                value, Request.Query["nice"] == "1" ? Formatting.Indented : Formatting.None
                                ) ?? "null", "application/json");
                    }
                }
                catch (DataSetException)
                {
                    return Json(ApiResponseStatus.DatasetNotFound);
                }
                catch (Exception ex)
                {
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));
                }

            }
        }



        [HttpGet, ActionName("DatasetItem_Exists")]
        public ActionResult DatasetItem_Exists(string _id, string _dataid)
        {
            string id = _id;
            string dataid = _dataid;

            if (!Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("dataid", dataid)
                })
                .Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {
                try
                {
                    var ds = DataSet.CachedDatasets.Get(id.ToLower());
                    bool value = ds.ItemExists(dataid);
                    //remove from item
                    if (value == false)
                        return Content(JsonConvert.SerializeObject(false), "application/json");
                    else
                        return Content(JsonConvert.SerializeObject(true), "application/json");
                }
                catch (DataSetException)
                {
                    return Json(ApiResponseStatus.DatasetNotFound);
                }
                catch (Exception ex)
                {
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));
                }

            }
        }


        [HttpPost, ActionName("DatasetItem")]
        public ActionResult DatasetItem_Post(string _id, string _dataid, [FromBody] Object body, string mode = "", bool? rewrite = false) //rewrite for backwards compatibility
        {
            string id = _id;
            string dataid = _dataid;

            var apiAuth = Framework.ApiAuth.IsApiAuth(HttpContext,
                parameters: new Framework.ApiCall.CallParameter[] {
                    new Framework.ApiCall.CallParameter("id", id),
                    new Framework.ApiCall.CallParameter("dataid", dataid),
                    new Framework.ApiCall.CallParameter("mode", mode)
                });
            if (!apiAuth.Authentificated)
            {
                //Response.StatusCode = 401;
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);
            }
            else
            {

                mode = mode.ToLower();
                if (string.IsNullOrEmpty(mode))
                {
                    if (rewrite == true)
                        mode = "rewrite";
                    else
                        mode = "skip";
                }

                string data = body.ToString();
                //var data = ReadRequestBody(Request);
                id = id.ToLower();
                try
                {
                    var ds = DataSet.CachedDatasets.Get(id);
                    var newId = dataid;

                    if (mode == "rewrite")
                    {
                        newId = ds.AddData(data, dataid, apiAuth.ApiCall.User, true);
                    }
                    else if (mode == "merge")
                    {
                        if (ds.ItemExists(dataid))
                        {
                            //merge
                            var oldObj = Datasets.Util.CleanHsProcessTypeValuesFromObject(ds.GetData(dataid));
                            var newObj = Datasets.Util.CleanHsProcessTypeValuesFromObject(data);

                            newObj["DbCreated"] = oldObj["DbCreated"];
                            newObj["DbCreatedBy"] = oldObj["DbCreatedBy"];

                            var diffs = Datasets.Util.CompareObjects(oldObj, newObj);
                            if (diffs.Count > 0)
                            {
                                oldObj.Merge(newObj,
                                new Newtonsoft.Json.Linq.JsonMergeSettings()
                                {
                                    MergeArrayHandling = Newtonsoft.Json.Linq.MergeArrayHandling.Union,
                                    MergeNullValueHandling = Newtonsoft.Json.Linq.MergeNullValueHandling.Ignore
                                }
                                );
                                newId = ds.AddData(oldObj.ToString(), dataid, apiAuth.ApiCall.User, true);
                            }
                        }
                        else
                            newId = ds.AddData(data, dataid, apiAuth.ApiCall.User, true);


                    }
                    else //skip 
                    {
                        if (!ds.ItemExists(dataid))
                            newId = ds.AddData(data, dataid, apiAuth.ApiCall.User, true);
                    }
                    return Json(new { id = newId });
                }
                catch (DataSetException dse)
                {
                    return Json(dse.APIResponse);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Error("Dataset API", ex);
                    return Json(ApiResponseStatus.GeneralExceptionError(ex));

                }


            }
        }


    }
}


