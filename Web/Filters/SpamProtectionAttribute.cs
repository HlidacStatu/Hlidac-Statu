using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;
using System.Collections.Generic;

namespace HlidacStatu.Web.Filters
{
    public class SpamProtectionAttribute : TypeFilterAttribute
    {
        private static readonly ILogger _logger = Log.ForContext<SpamProtectionAttribute>();
        public SpamProtectionAttribute(string name, string redirectToController, string redirectToAction) : base(typeof(SpamProtectionImpl))
        {
            // careful about correct ordering. It has to be the same as in the constructor below
            Arguments = new object[] { name, redirectToAction, redirectToController };
        }

        private class SpamProtectionImpl : IActionFilter
        {

            private readonly string _name;
            private readonly IActionResult _redirect;

            public SpamProtectionImpl(string name, string redirectToAction, string redirectToController)
            {
                _name = string.IsNullOrWhiteSpace(name) ? Framework.Constants.AntispamInputName : name;
                _redirect = new RedirectToActionResult(redirectToAction, redirectToController, null);
            }

            public void OnActionExecuting(ActionExecutingContext context)
            {
                var req = context.HttpContext.Request;

                // can't parse body in action filter (https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/filters?view=aspnetcore-5.0#next-actions)
                if (IsInFormData(req) || IsInQueryData(req))// ||  await IsInBodyJson(req)) 
                {
                    //context.HttpContext.Items.Add("honeypotTrapped", true);
                    _logger.Warning($"Detected bot from [{HlidacStatu.Util.RealIpAddress.GetIp(context.HttpContext)}]");
                    _=context.HttpContext.Items.TryAdd(HlidacStatu.LibCore.MiddleWares.BannedIpsMiddleware.ContextKeyNameBotWarning,50);
                    context.Result = _redirect;
                }
            }

            public void OnActionExecuted(ActionExecutedContext context)
            {
            }

            private bool IsInFormData(HttpRequest request)
            {
                return request.HasFormContentType
                       && request.Form.TryGetValue(_name, out var data)
                       && !string.IsNullOrEmpty(data.ToString());
            }

            private bool IsInQueryData(HttpRequest request)
            {
                return request.Query.TryGetValue(_name, out var data)
                    && !string.IsNullOrEmpty(data.ToString());
            }

            // limitation: doesnt check for nested json objects. Checks only for properties in root
            // limitation: currently doesnt work
            // private async MonitoredTask<bool> IsInBodyJson(HttpRequest request)
            // {
            //     if (request.HasFormContentType)
            //         return false;
            //     
            //     request.EnableBuffering();
            //     
            //     try
            //     {
            //         using var json = await JsonDocument.ParseAsync(request.Body);
            //
            //         return json.RootElement.TryGetProperty(_name, out var x);
            //     }
            //     finally
            //     {
            //         request.Body.Position = 0;
            //     }
            // }

        }

    }
}