using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhuThuongStickyRice.Infrastructure.Web.Authorization.Requirements
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionName { get; set; }
    }

    public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
    {
        /// <summary>Claim type issued in our JWTs for per-permission grants.</summary>
        public const string PermissionClaimType = "permission";

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var user = context.User;

            if (user?.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            // Admins hold every permission; other users need an explicit
            // "permission" claim matching the required policy name.
            if (user.IsInRole("Admin")
                || user.HasClaim(PermissionClaimType, requirement.PermissionName))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
