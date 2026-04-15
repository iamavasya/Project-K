using System.Security.Claims;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.API.Helpers;

public sealed class HttpCurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        ParseGuid(
            Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Principal?.FindFirstValue("sub"));

    public Guid? KurinKey => ParseGuid(Principal?.FindFirstValue("kurinKey"));

    public IReadOnlyCollection<string> Roles =>
        Principal?
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? [];

    public bool IsInRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    private static Guid? ParseGuid(string? value)
    {
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
