using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Web.Framework;
using HlidacStatu.Web.Framework.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;

namespace HlidacStatu.Web.Filters
{
    public class AuthorizeAndAuditAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// Gets or sets user roles which are authorized to access method.
        /// </summary>
        public string Roles { get; set; }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var controller = (IAuthenticableController)context.Controller;

            var parameters = context.ActionArguments
                .Select(ap => new ApiCall.CallParameter(ap.Key, JsonConvert.SerializeObject(ap.Value)))
                .ToList();
            
            var apiAuth = ApiAuth.IsApiAuth(context.HttpContext,
                validRole: Roles,
                parameters: parameters);

            if (!apiAuth.Authentificated)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            controller.ApiAuth = apiAuth;

            await next();

        }

    }
}