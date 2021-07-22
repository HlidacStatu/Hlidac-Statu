using HlidacStatu.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Http;

namespace HlidacStatu.Web.Framework
{
    public class ApiCall : IAuditable
    {
        public class CallParameter
        {
            public string Name { get; set; }
            public string Value { get; set; }

            public CallParameter() { }
            public CallParameter(string name, string value)
            {
                Name = name;
                Value = value;
            }

            public static implicit operator CallParameter(KeyValuePair<string, string> kv)
            {
                return new CallParameter() { Name = kv.Key, Value = kv.Value };
            }

        }
        public string Method { get; set; }
        public string Id { get; set; }
        public IEnumerable<CallParameter> Parameters { get; set; }

        /// <summary>
        /// User's email
        /// </summary>
        public string User { get; set; }
        public string UserId { get; set; }

        public string IP { get; set; }

        public string[] UserRoles { get; set; } = new string[] { };


        public string ToAuditJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public string ToAuditObjectId()
        {
            return Id;
        }

        public string ToAuditObjectTypeName()
        {
            return "ApiCall";
        }
    }


    public class ApiAuth
    {
        private static string[] dontAuditMethods = new string[] { "getforclassification", "classificationlist" };
        public class Result
        {
            public static Result Valid(ApiCall apiCall)
            {

                if (!dontAuditMethods.Contains(apiCall.Method.ToLower()))
                {
                    AuditRepo.Add(Audit.Operations.Call, apiCall.User, apiCall.IP, apiCall, null);
                }
                return new Result(true, apiCall);
            }
            public static Result Invalid(ApiCall apiCall)
            {
                if (!string.IsNullOrEmpty(apiCall?.User))
                {
                    AuditRepo.Add(Audit.Operations.InvalidAccess, apiCall.User, apiCall.IP, apiCall, null);
                }
                else
                {
                    AuditRepo.Add(Audit.Operations.InvalidAccess, apiCall.User, apiCall.IP, apiCall, null);
                }
                return new Result(false, apiCall);
            }

            private Result(bool valid, ApiCall apiCall)
            {
                Authentificated = valid;
                ApiCall = apiCall;
            }
            public bool Authentificated { get; private set; } = false;
            public ApiCall ApiCall { get; private set; } = null;

        }

        public static Result IsApiAuth(HttpContext httpContext, string validRole = null, IEnumerable<ApiCall.CallParameter> parameters = null, [CallerMemberName] string method = "")
        {
            if (string.IsNullOrEmpty(validRole))
                return IsApiAuth(httpContext, new string[] { }, parameters, method);
            else
                return IsApiAuth(httpContext, validRole.Split(','), parameters, method);
        }

        public static Result IsApiAuth(HttpContext httpContext, string[] validRoles, IEnumerable<ApiCall.CallParameter> parameters = null, [CallerMemberName] string method = "")
        {
            if (IsApiAuthHeader(httpContext.GetAuthToken(), out string login))
            {
                ApplicationUser user = ApplicationUser.GetByEmail(login);
                if (user == null)
                    return Result.Invalid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId=null, User = null, Id = method, Method = method, Parameters = parameters });
                else
                {
                    var userroles = user.GetRoles();

                    if (validRoles == null)
                        return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user.Id, User = user.Email, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                    else if (validRoles.Count() == 0)
                        return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user.Id, User = user.Email, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                    else
                    {
                        foreach (var role in validRoles)
                        {
                            if (user.IsInRole(role.Trim()))
                                return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user.Id, User = user.Email, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                        }
                        return Result.Invalid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user.Id, User = user.Email, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                    }

                }

            }
            else if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                ApplicationUser user = ApplicationUser.GetByEmail(httpContext.User.Identity.Name);

                var userroles = user.GetRoles();

                if (validRoles == null)
                    return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user?.Id, User = httpContext.User.Identity.Name, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                else if (validRoles.Count() == 0)
                    return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user?.Id, User = httpContext.User.Identity.Name, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                else
                {
                    foreach (var role in validRoles)
                    {
                        if (httpContext.User.IsInRole(role.Trim()))
                            return Result.Valid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user?.Id, User = httpContext.User.Identity.Name, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                    }
                    return Result.Invalid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId = user?.Id, User = httpContext.User.Identity.Name, Id = method, Method = method, Parameters = parameters, UserRoles = userroles });
                }
            }
            else
                return Result.Invalid(new ApiCall() { IP = httpContext.GetRemoteIp(), UserId=null, User = null, Id = method, Method = method, Parameters = parameters });
        }

        private static bool IsApiAuthHeader(string authToken, out string login)
        {
            login = "";
            
            if (string.IsNullOrEmpty(authToken))
                return false;
            authToken = authToken.Replace("Token ", "").Trim();

            if (Guid.TryParse(authToken, out var guid))
            {
                using (DbEntities db = new())
                {
                    var user = db.AspNetUserApiTokens.AsQueryable().FirstOrDefault(m => m.Token == guid);
                    if (user != null)
                        login = db.Users.AsQueryable().FirstOrDefault(m => m.Id == user.Id)?.Email;
                    return user != null;
                }
            }
            else
                return false;
        }
    
    }
}