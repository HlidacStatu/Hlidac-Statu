using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Collections.Generic;

namespace HlidacStatu.Web.Filters
{
    public class SpamProtectionRazor : IPageFilter
    {
        private static readonly ILogger _logger = Log.ForContext<SpamProtectionRazor>();
        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var req = context.HttpContext.Request;

            if (IsInFormData(req) || IsInQueryData(req))
            {
                _ = context.HttpContext.Items.TryAdd(HlidacStatu.LibCore.MiddleWares.BannedIpsMiddleware.ContextKeyNameBotWarning, 50);
                _logger.Warning($"Detected bot from [{HlidacStatu.Util.RealIpAddress.GetIp(context.HttpContext)}] filling in 'email2' field value.");
                context.Result = new RedirectToActionResult("Bot", "Error", null);
            }
        }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
        {
        }

        private bool IsInFormData(HttpRequest request)
        {
            return request.HasFormContentType
                   && request.Form.TryGetValue(Framework.Constants.AntispamInputName, out var data)
                   && !string.IsNullOrEmpty(data.ToString());
        }

        private bool IsInQueryData(HttpRequest request)
        {
            return request.Query.TryGetValue(Framework.Constants.AntispamInputName, out var data)
                   && !string.IsNullOrEmpty(data.ToString());
        }

    }
}