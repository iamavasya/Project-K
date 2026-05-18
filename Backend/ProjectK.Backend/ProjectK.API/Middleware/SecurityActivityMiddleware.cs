using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.API.Middleware
{
    public sealed class SecurityActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IActivityLogger activityLogger)
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? context.User.FindFirstValue("sub");

            if (Guid.TryParse(userIdValue, out var userId))
            {
                var ip = context.Connection.RemoteIpAddress?.ToString();
                if (!string.IsNullOrWhiteSpace(ip))
                {
                    activityLogger.TrackIpChange(userId, ip);
                }
            }

            await _next(context);
        }
    }
}
