using HlidacStatu.Datasets;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace HlidacStatu.Web.Controllers
{
    public partial class ApiV1Controller : Controller
    {
        [Authorize]
        public ActionResult Watchdog(string _id, string _dataid, string dataType = "VerejnaZakazka",
            string? query = null, string? expiration = null)
        {
            string id = _id;
            string dataid = _dataid;

            id = id.ToLower();
            
            if (HttpContext.User.Identity.Name != "michal@michalblaha.cz"
                && HttpContext.User.Identity.Name != "maixner@solidis.cz")
                return Json(ApiResponseStatus.ApiUnauthorizedAccess);

            var wdName = WatchDog.APIID_Prefix + dataid;
            using (DbEntities db = new DbEntities())
            {
                switch (id.ToLower())
                {
                    case "add":
                        var expirDate = Devmasters.DT.Util.ToDateTime(expiration, "yyyy-MM-ddTHH:mm:ss");
                        if (string.IsNullOrEmpty(query))
                        {
                            return Json(ApiResponseStatus.Error(-99, "No query"));
                        }

                        var wd2 = db.WatchDogs.AsNoTracking().Where(m => m.Name == wdName).FirstOrDefault();
                        if (wd2 != null)
                        {
                            wd2.SearchTerm = query;
                            wd2.Expires = expirDate;
                            WatchdogRepo.Save(wd2);
                        }
                        else
                        {
                            var dt = dataType;

                            WatchDog wd = new WatchDog();
                            wd.Created = DateTime.Now;
                            wd.UserId = HttpContext.User?.Claims?
                                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?
                                .Value;
                            wd.StatusId = 1;
                            wd.SearchTerm = query;
                            wd.PeriodId = 2; //daily
                            wd.FocusId = 0;
                            wd.Name = wdName;
                            wd.Expires = expirDate;
                            wd.SpecificContact = "HTTPPOSTBACK";
                            if (dt.ToLower() == typeof(Smlouva).Name.ToLower())
                                wd.DataType = typeof(Smlouva).Name;
                            else if (dt.ToLower() == typeof(VerejnaZakazka).Name.ToLower())
                                wd.DataType = typeof(VerejnaZakazka).Name;
                            else if (dt.ToLower().StartsWith(typeof(DataSet).Name.ToLower()))
                            {
                                var dataSetId = dt.Replace("DataSet.", "");
                                if (DataSet.ExistsDataset(dataSetId) == false)
                                {
                                    Util.Consts.Logger.Error("AddWd - try to hack, wrong dataType = " + dataType +
                                                             "." + dataSetId);
                                    throw new ArgumentOutOfRangeException("AddWd - try to hack, wrong dataType = " +
                                                                          dataType + "." + dataSetId);
                                }

                                wd.DataType = typeof(DataSet).Name + "." + dataSetId;
                            }
                            else if (dt == WatchDog.AllDbDataType)
                            {
                                wd.DataType = dt;
                            }
                            else
                            {
                                Util.Consts.Logger.Error("AddWd - try to hack, wrong dataType = " + dataType);
                                throw new ArgumentOutOfRangeException("AddWd - try to hack, wrong dataType = " +
                                                                      dataType);
                            }

                            wd.Save();
                        }

                        break;
                    case "delete":
                    case "disable":
                    case "get":
                    case "enable":
                        var wd1 = db.WatchDogs.AsNoTracking().Where(m => m.Name == wdName).FirstOrDefault();
                        if (wd1 == null)
                            return Json(ApiResponseStatus.Error(-404, "Watchdog not found"));

                        if (id == "delete")
                        {
                            wd1.Delete();
                            return Json(new { Ok = true });
                        }

                        if (id == "disable")
                            wd1.StatusId = 0;
                        if (id == "delete")
                            wd1.StatusId = 1;
                        if (id == "get")
                            return Content(
                                Newtonsoft.Json.JsonConvert.SerializeObject(new
                                {
                                    id = wd1.Name.Replace("APIID:", ""),
                                    expiration = wd1.Expires,
                                    query = wd1.SearchTerm
                                }), "text/json");

                        wd1.Save();
                        break;
                    default:
                        break;
                }
            }

            return Json(new { Ok = true });
        }
    }
}