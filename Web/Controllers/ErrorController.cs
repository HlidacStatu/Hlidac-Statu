using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("error")]
    public class ErrorController : Controller
    {
        [Route("500")]
        public IActionResult ApplicationError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            string err = HttpContext.Items[HlidacStatu.LibCore.MiddleWares.OnHTTPErrorMiddleware.ItemKeyName] as string;

            Util.Consts.Logger.Error($"500 - Server error on {exceptionHandlerPathFeature.Path}\n\n" + err);

            return View();
        }

        [Route("404")]
        public IActionResult PageNotFound()
        {
            string err = HttpContext.Items[HlidacStatu.LibCore.MiddleWares.OnHTTPErrorMiddleware.ItemKeyName] as string;
            
            Util.Consts.Logger.Warning($"HTTP 404 - Page not found.\n\n" + err);
            var errObj = HttpContext.Items[HlidacStatu.LibCore.MiddleWares.OnHTTPErrorMiddleware.ItemKeyNameObj] as Dictionary<string, string>;

            if (errObj != null)
                Util.Consts.Logger.Error("HTTP 404 - page not found, context {context}",errObj);

            return View();
        }

        [Route("{code:int}")]
        public IActionResult GeneralError(int code)
        {
            string err = HttpContext.Items[HlidacStatu.LibCore.MiddleWares.OnHTTPErrorMiddleware.ItemKeyName] as string;

            string? originalPath = "unknown";
            if (HttpContext.Items.ContainsKey("originalPath"))
            {
                originalPath = HttpContext.Items["originalPath"] as string;
            }
            Util.Consts.Logger.Warning($"{code} error - [{originalPath}].\n\n" + err);

            return View(code);
        }

        public IActionResult Bot()
        {
            return View();
        }
    }
}