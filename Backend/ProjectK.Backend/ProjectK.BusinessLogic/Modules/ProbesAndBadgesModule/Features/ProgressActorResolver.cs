using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;

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

    private static string ResolveRole(ICurrentUserContext currentUserContext)
    {
        if (currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()))
        {
            return UserRole.Admin.ToClaimValue();
        }

        if (currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()))
        {
            return UserRole.Manager.ToClaimValue();
        }

        if (currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
        {
            return UserRole.Mentor.ToClaimValue();
        }

        if (currentUserContext.IsInRole(UserRole.User.ToClaimValue()))
        {
            return UserRole.User.ToClaimValue();
        }

        return currentUserContext.Roles.FirstOrDefault() ?? "Unknown";
    }
}
