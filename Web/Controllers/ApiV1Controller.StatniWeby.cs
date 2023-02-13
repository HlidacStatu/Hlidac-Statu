using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HlidacStatu.Web.Controllers
{

    public partial class ApiV1Controller : Controller
    {
        [Authorize]
        public ActionResult WebList()
        {
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers()
                ), "text/json");
        }

        //[Authorize]
        //public async MonitoredTask<ActionResult> WebStatus(string _id)
        //{
        //    string id = _id;

        //    if (Devmasters.TextUtil.IsNumeric(id))
        //        return await DataHostAsync(Convert.ToInt32(id));
        //    else
        //        return Json(ApiResponseStatus.StatniWebNotFound);
        //}

        //private async MonitoredTask<ActionResult> DataHostAsync(int id)
        //{
        //    UptimeServer host = UptimeServerRepo.AllActiveServers().Where(w => w.Id == id).FirstOrDefault();
        //    if (host == null)
        //        return Json(ApiResponseStatus.StatniWebNotFound);

        //    try
        //    {
        //        UptimeServer.HostAvailability? data = UptimeServerRepo.AvailabilityForDayById(host.Id);
        //        UptimeSSL? webssl = await UptimeSSLRepo.LoadLatestAsync(host.HostDomain());
        //        var ssldata = new
        //        {
        //            grade = webssl == null ? null : webssl.SSLGrade().ToNiceDisplayName(),
        //            time = webssl?.Created,
        //            expiresAt = webssl?.CertExpiration()
        //        };
        //        if (webssl == null)
        //        {
        //            ssldata = null;
        //        }
        //        return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
        //            new
        //            {
        //                availability = data,
        //                ssl = ssldata
        //            })
        //            , "text/json");
        //    }
        //    catch (Exception e)
        //    {
        //        HlidacStatu.Util.Consts.Logger.Error($"_DataHost id ${id}", e);
        //        return Json(ApiResponseStatus.GeneralExceptionError(e));
        //    }

        //}


    }
}


