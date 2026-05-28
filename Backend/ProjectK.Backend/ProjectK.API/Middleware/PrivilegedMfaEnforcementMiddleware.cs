using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Middleware
{
    public class PrivilegedMfaEnforcementMiddleware
    {
        private readonly RequestDelegate _next;

        public PrivilegedMfaEnforcementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager, IHostEnvironment environment, IConfiguration configuration)
        {
            if (environment.IsDevelopment() || configuration.GetValue<bool>("E2E:BypassPrivilegedMfa", false) || !RequiresMfaEnforcement(context))
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userId, out var userKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var user = await userManager.FindByIdAsync(userKey.ToString());
            if (user?.TwoFactorEnabled == true)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "MFA is required for privileged accounts."
            });
        }

        private static bool RequiresMfaEnforcement(HttpContext context)
        {
            if (HttpMethods.IsOptions(context.Request.Method)
                || HttpMethods.IsHead(context.Request.Method)
                || HttpMethods.IsGet(context.Request.Method)
                || context.User.Identity?.IsAuthenticated != true
                || IsExemptPath(context))
            {
                return false;
            }

            return context.User.IsInRole(UserRole.Admin.ToString())
                || context.User.IsInRole(UserRole.Manager.ToString());
        }

        private static bool IsExemptPath(PathString path)
        {
            return path.StartsWithSegments("/api/auth/mfa", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/auth/logout", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/api/auth/refresh", StringComparison.OrdinalIgnoreCase)
                || path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExemptPath(HttpContext context)
        {
            var path = context.Request.Path;
            if (IsExemptPath(path))
            {
                return true;
            }

            return (HttpMethods.IsPost(context.Request.Method)
                    && path.StartsWithSegments("/api/user/me/mfa/reset", StringComparison.OrdinalIgnoreCase))
                || (HttpMethods.IsPost(context.Request.Method)
                    && path.StartsWithSegments("/api/auth/check-access", StringComparison.OrdinalIgnoreCase));
        }
    }
}
