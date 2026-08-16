using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Extensions;

/// <summary>
/// Permission-derived questions handlers ask about the current user, so no handler inspects role
/// names directly. All of these expand the user's roles through <see cref="RolePermissionMap"/>.
/// </summary>
public static class CurrentUserPermissionExtensions
{
    public static bool IsAdmin(this ICurrentUserContext user) => user.IsInRole(SystemRole.Admin);

    public static IReadOnlyCollection<Permission> Permissions(this ICurrentUserContext user) =>
        RolePermissionMap.Resolve(user.Roles ?? Array.Empty<string>());

    /// <summary>True for the whole-kurin managers (Зв'язковий, Курінний, admin).</summary>
    public static bool CanManageWholeKurin(this ICurrentUserContext user) =>
        RolePermissionMap.WidestScope(user.Permissions(), ResourceType.Group, ResourceAction.Manage) == AccessScope.KurinWide;

    /// <summary>True for anyone who leads groups (гуртковий leaders plus whole-kurin managers).</summary>
    public static bool CanLeadGroups(this ICurrentUserContext user) =>
        RolePermissionMap.WidestScope(user.Permissions(), ResourceType.Group, ResourceAction.Update) is not null;

    /// <summary>Whole-kurin managers or group leaders — the historic "leadership" set.</summary>
    public static bool IsLeadership(this ICurrentUserContext user) =>
        user.CanManageWholeKurin() || user.CanLeadGroups();

    /// <summary>Whether the user is granted (resource, action) at any scope.</summary>
    public static bool HasPermission(this ICurrentUserContext user, ResourceType resource, ResourceAction action) =>
        RolePermissionMap.WidestScope(user.Permissions(), resource, action) is not null;
}
