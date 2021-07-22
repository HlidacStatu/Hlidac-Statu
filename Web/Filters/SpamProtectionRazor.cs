using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HlidacStatu.Web.Filters
{
    public class SpamProtectionRazor : IPageFilter
    {
        
        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
        {
            var req = context.HttpContext.Request;

            if (IsInFormData(req) || IsInQueryData(req)) 
            {
                Util.Consts.Logger.Warning($"Detected bot from [{context.HttpContext.Connection.RemoteIpAddress}] filling in 'email2' field value.");
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