using Devmasters.Enums;

using HlidacStatu.Datasets;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

namespace HlidacStatu.Web.Controllers
{

    //migrace: https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-5.0
    //migrace: komprese by měla být přenechána modulu na iis
    public partial class ApiV1Controller : Controller
    {
        [Authorize]
        public ActionResult WebList()
        {
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers()
                ), "text/json");
        }

        [Authorize]
        public ActionResult WebStatus(string _id, string h)
        {
            string id = _id;

            if (Devmasters.TextUtil.IsNumeric(id))
                return _DataHost(Convert.ToInt32(id), h);
            else
                return Json(ApiResponseStatus.StatniWebNotFound);
        }

        private ActionResult _DataHost(int id, string h)
        {
            UptimeServer host = UptimeServerRepo.AllActiveServers().Where(w => w.Id == id).FirstOrDefault();
            if (host == null)
                return Json(ApiResponseStatus.StatniWebNotFound);

            if (host.ValidHash(h))
            {
                try
                {
                    UptimeServer.HostAvailability? data = UptimeServerRepo.AvailabilityForDayById(host.Id);
                    UptimeSSL? webssl = UptimeSSLRepo.LoadLatestAsync(host.HostDomain());
                    var ssldata = new
                    {
                        grade = webssl==null ? null : webssl.SSLGrade().ToNiceDisplayName(),
                        time = webssl?.Created,
                        expiresAt = webssl?.CertExpiration()
                    };
                    if (webssl == null)
                    {
                        ssldata = null;
                    }
                    return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                        new
                        {
                            availability = data,
                            ssl = ssldata
                        })
                        , "text/json");

                }
                catch (Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Error($"_DataHost id ${id}", e);
                    return Json(ApiResponseStatus.GeneralExceptionError(e));
                }

            }
            else
                return Json(ApiResponseStatus.StatniWebNotFound);
        }
    }
}


