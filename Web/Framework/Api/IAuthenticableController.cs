using System.Security.Claims;
using HlidacStatu.Entities;

namespace HlidacStatu.Web.Framework
{
    public interface IAuthenticableController
    {
        ClaimsPrincipal User { get; }
        string HostIpAddress { get; }
        string AuthToken { get; }
        
        //used only in apiV2
        ApiAuth.Result ApiAuth { get; set; }

        ApplicationUser AuthUser();
    }
}
