using ProjectK.Common.Models.Enums;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features;

internal static class ProgressActorResolver
{
    public static (Guid? UserKey, string? ActorName, string ActorRole) Resolve(ICurrentUserContext currentUserContext)
    {
        return (
            currentUserContext.UserId,
            currentUserContext.UserId?.ToString(),
            ResolveRole(currentUserContext));
    }

    /// <summary>
    /// The office recorded in the audit trail: admin, otherwise the one that actually authorised the
    /// action — the widest <c>Member:Update</c> scope among the offices held — and the bare baseline
    /// when none does. Ties break by name so the same user is always recorded the same way; this used
    /// to take whichever office the identity store happened to return first.
    /// </summary>
    private static string ResolveRole(ICurrentUserContext currentUserContext)
    {
        if (currentUserContext.IsInRole(SystemRole.Admin))
        {
            return SystemRole.Admin;
        }

        var office = currentUserContext.Roles
            .Where(role => !string.Equals(role, SystemRole.Member, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(role => RolePermissionMap.WidestScope(
                RolePermissionMap.Resolve(new[] { role }),
                ResourceType.Member,
                ResourceAction.Update) ?? default)
            .ThenBy(role => role, StringComparer.Ordinal)
            .FirstOrDefault();

        return office ?? SystemRole.Member;
    }
}
