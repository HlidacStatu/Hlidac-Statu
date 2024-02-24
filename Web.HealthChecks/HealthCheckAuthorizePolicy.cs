using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class HealthCheckAuthorizePolicy : IAuthorizationPolicyProvider
    {
        public const string POLICY_NAME = "HealthCheckAuthorizePolicy_";

        public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

        public HealthCheckAuthorizePolicy(IOptions<AuthorizationOptions> options)
        {
            FallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => FallbackPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => FallbackPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith(POLICY_NAME))
            {
                var roleName = policyName.Substring(POLICY_NAME.Length);
                if (!string.IsNullOrEmpty(roleName) )
                {
                    var policy = new AuthorizationPolicyBuilder();
                    policy.AddRequirements(new UserInRoleRequirement(roleName));
                    return Task.FromResult(policy.Build());
                }
            }
            return FallbackPolicyProvider.GetPolicyAsync(policyName);

        }
        }
        internal class UserInRoleRequirement : IAuthorizationRequirement
        {
            public string Rolename { get; private set; }

            public UserInRoleRequirement(string paramName) { 
                Rolename = paramName;
            }
        }

    internal class IsInRoleRequirementHandler : AuthorizationHandler<UserInRoleRequirement>
    {
        //private readonly ILogger<UrlParameterRequirementHandler> _logger;

        public IsInRoleRequirementHandler()
        {
        }

        // Check whether a given MinimumAgeRequirement is satisfied or not for a particular context
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserInRoleRequirement requirement)
        {
            // Log as a warning so that it's very clear in sample output which authorization policies 
            // (and requirements/handlers) are in use
            //_logger.LogWarning("Evaluating authorization requirement for age >= {age}", requirement.Age);

            // Check the user's age
          /*  var ctxName = context.User.IsInRole("admin");

            var dateOfBirthClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
            if (dateOfBirthClaim != null)
            {
                // If the user has a date of birth claim, check their age
                var dateOfBirth = Convert.ToDateTime(dateOfBirthClaim.Value);
                var age = DateTime.Now.Year - dateOfBirth.Year;
                if (dateOfBirth > DateTime.Now.AddYears(-age))
                {
                    // Adjust age if the user hasn't had a birthday yet this year
                    age--;
                }

                // If the user meets the age criterion, mark the authorization requirement succeeded
                if (age >= requirement.Age)
                {
                    _logger.LogInformation("Minimum age authorization requirement {age} satisfied", requirement.Age);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogInformation("Current user's DateOfBirth claim ({dateOfBirth}) does not satisfy the minimum age authorization requirement {age}",
                        dateOfBirthClaim.Value,
                        requirement.Age);
                }
            }
            else
            {
                _logger.LogInformation("No DateOfBirth claim present");
            }
*/
            return Task.CompletedTask;
        }
    }
}

