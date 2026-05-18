using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using ProjectK.Common.Extensions;
using ProjectK.Common.Models.Enums;

namespace ProjectK.API.Services.Authorization;

public sealed class AdminOrServiceTokenRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "RequireAdminOrServiceToken";
    public const string HeaderName = "X-ProjectK-Service-Token";
    public const string ConfigKey = "AdminServiceToken:PublicAnnouncementDraft";
}

public sealed class AdminOrServiceTokenHandler : AuthorizationHandler<AdminOrServiceTokenRequirement>
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminOrServiceTokenHandler(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminOrServiceTokenRequirement requirement)
    {
        if (context.User?.IsInRole(UserRole.Admin.ToClaimValue()) == true)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return Task.CompletedTask;
        }

        var expectedToken = _configuration[AdminOrServiceTokenRequirement.ConfigKey]?.Trim();
        if (string.IsNullOrWhiteSpace(expectedToken))
        {
            return Task.CompletedTask;
        }

        if (!httpContext.Request.Headers.TryGetValue(AdminOrServiceTokenRequirement.HeaderName, out var providedHeader))
        {
            return Task.CompletedTask;
        }

        var providedToken = providedHeader.ToString().Trim();
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            return Task.CompletedTask;
        }

        if (FixedTimeEquals(expectedToken, providedToken))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        if (expectedBytes.Length != providedBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}
