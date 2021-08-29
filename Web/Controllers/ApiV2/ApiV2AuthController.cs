using HlidacStatu.Entities;
using HlidacStatu.Web.Framework;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace HlidacStatu.Web.Controllers
{
    public class ApiV2AuthController : ControllerBase, IAuthenticableController
    {
        public string HostIpAddress => HttpContext.GetRemoteIp();

        public string AuthToken
        {
            get
            {
                var auth = Request.Headers[HeaderNames.Authorization];
                if (StringValues.IsNullOrEmpty(auth))
                    return auth;

                return Request.Query["Authorization"];
            }
        }

        [NonAction]
        public ApplicationUser AuthUser()
        {
            ApplicationUser user = ApplicationUser.GetByEmail(ApiAuth.ApiCall.User);
            return user;
        }

        public ApiAuth.Result ApiAuth { get; set; }

    }
}