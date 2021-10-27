using Devmasters.Enums;

using HlidacStatu.Datasets;
using HlidacStatu.Lib.Data.External.Zabbix;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

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
                HlidacStatu.Lib.Data.External.Zabbix.ZabTools.Weby()
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
            ZabHost host = ZabTools.Weby().Where(w => w.hostid == id.ToString() & w.itemIdResponseTime != null).FirstOrDefault();
            if (host == null)
                return Json(ApiResponseStatus.StatniWebNotFound);

            if (host.ValidHash(h))
            {
                try
                {
                    var data = ZabTools.GetHostAvailabilityLong(host);
                    var webssl = ZabTools.SslStatusForHostId(host.hostid);
                    var ssldata = new
                    {
                        grade = webssl?.Status().ToNiceDisplayName(),
                        time = webssl?.Time,
                        copyright = "(c) © Qualys, Inc. https://www.ssllabs.com/",
                        fullreport = "https://www.ssllabs.com/ssltest/analyze.html?d=" + webssl?.Host?.UriHost()
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


