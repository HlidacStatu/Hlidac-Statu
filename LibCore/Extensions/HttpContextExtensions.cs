using Microsoft.AspNetCore.Http;

namespace HlidacStatu.LibCore.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetAuthToken(this HttpContext httpContext)
        {
            if (httpContext.Request.Headers.ContainsKey("Authorization"))
            {
                return httpContext.Request.Headers["Authorization"];
            }
            else if (httpContext.Request.Query.ContainsKey("Authorization"))
            {
                return httpContext.Request.Query["Authorization"];
            }

            return "";
        }
        
        public static string GetRemoteIp(this HttpContext httpContext)
        {
            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown IP";
        }
    }
}