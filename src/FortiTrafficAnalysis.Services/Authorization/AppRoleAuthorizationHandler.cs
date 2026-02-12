using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FortiTrafficAnalysis.Services.Authorization
{
    /// <summary>
    /// Custom authorization requirement for application roles
    /// </summary>
    public class AppRoleRequirement : IAuthorizationRequirement
    {
        public string[] AllowedRoles { get; }

        public AppRoleRequirement(params string[] allowedRoles)
        {
            AllowedRoles = allowedRoles;
        }
    }

    /// <summary>
    /// Handler for application role authorization
    /// </summary>
    public class AppRoleAuthorizationHandler : AuthorizationHandler<AppRoleRequirement>
    {
        private readonly IUserAuthorizationService _userAuthService;

        public AppRoleAuthorizationHandler(IUserAuthorizationService userAuthService)
        {
            _userAuthService = userAuthService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            AppRoleRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return;
            }

            var upn = context.User.FindFirst(ClaimTypes.Name)?.Value
                   ?? context.User.FindFirst("preferred_username")?.Value
                   ?? context.User.FindFirst(ClaimTypes.Upn)?.Value;

            if (string.IsNullOrEmpty(upn))
            {
                return;
            }

            var userRole = await _userAuthService.GetUserRoleAsync(upn);

            if (!string.IsNullOrEmpty(userRole) && 
                System.Array.Exists(requirement.AllowedRoles, role => 
                    role.Equals(userRole, System.StringComparison.OrdinalIgnoreCase)))
            {
                context.Succeed(requirement);
            }
        }
    }
}
