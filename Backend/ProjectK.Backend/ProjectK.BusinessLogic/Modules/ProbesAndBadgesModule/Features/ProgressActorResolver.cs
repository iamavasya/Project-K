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

    // Records who performed the action for the audit trail: admin, otherwise the highest office role
    // the user holds, otherwise the bare member baseline.
    private static string ResolveRole(ICurrentUserContext currentUserContext)
    {
        if (currentUserContext.IsInRole(SystemRole.Admin))
        {
            return SystemRole.Admin;
        }

        var office = currentUserContext.Roles
            .FirstOrDefault(role => !string.Equals(role, SystemRole.Member, StringComparison.OrdinalIgnoreCase));

        return office ?? SystemRole.Member;
    }
}
