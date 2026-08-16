using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Authorization;

/// <summary>
/// The single source of truth for what each <see cref="SystemRole"/> may do. To grant new access,
/// change a grant list here — nothing else reasons about role names. The tiers reproduce the historic
/// Manager/Mentor/User behaviour so the migration is behaviour-preserving:
/// <list type="bullet">
/// <item><b>Зв'язковий (KV)</b> — full kurin management incl. kurin settings and office assignment.</item>
/// <item><b>Курінний</b> — manages groups/members/planning kurin-wide, but not kurin settings nor office assignment.</item>
/// <item><b>Гуртковий</b> — manages only within the groups it leads.</item>
/// <item>every other office and the bare <b>Member</b> — read within the kurin plus own profile/progress.</item>
/// </list>
/// </summary>
public static class RolePermissionMap
{
    private static readonly ResourceType[] AllResources = Enum.GetValues<ResourceType>();

    // Baseline every authenticated member carries: read anything in the kurin, edit own profile,
    // submit own badge progress. Mirrors the old UserRole.User rules.
    private static readonly IReadOnlyList<Permission> MemberGrants = BuildMemberGrants();

    // Гуртковий: create/update within led groups on top of the baseline. Mirrors UserRole.Mentor.
    private static readonly IReadOnlyList<Permission> GroupLeadGrants = BuildGroupLeadGrants();

    // Курінний: kurin-wide management, minus kurin settings (Kurin.Update) and office assignment
    // (Leadership create/update/delete/manage).
    private static readonly IReadOnlyList<Permission> KurinLeadGrants = BuildKurinLeadGrants();

    // Зв'язковий: full kurin management. Mirrors UserRole.Manager (irreversible Kurin
    // Delete/Manage stays Admin-only).
    private static readonly IReadOnlyList<Permission> StewardGrants = BuildStewardGrants();

    // Admin: everything, kurin-wide. The scope check still pins a stepped-in admin to their kurin.
    private static readonly IReadOnlyList<Permission> AdminGrants = BuildAdminGrants();

    /// <summary>Expands a set of role names into the union of their permissions.</summary>
    public static IReadOnlyCollection<Permission> Resolve(IEnumerable<string> roles)
    {
        var permissions = new HashSet<Permission>();
        foreach (var role in roles)
        {
            foreach (var permission in GrantsFor(role))
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    /// <summary>Whether the roles grant whole-kurin management (Зв'язковий/Курінний/admin).</summary>
    public static bool GrantsWholeKurinManagement(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.Group, ResourceAction.Manage) == AccessScope.KurinWide;

    /// <summary>Whether the roles grant group leadership at any scope (гуртковий or higher).</summary>
    public static bool GrantsGroupLeadership(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.Group, ResourceAction.Update) is not null;

    /// <summary>The widest scope at which <paramref name="permissions"/> grants (resource, action), if any.</summary>
    public static AccessScope? WidestScope(
        IEnumerable<Permission> permissions,
        ResourceType resource,
        ResourceAction action)
    {
        AccessScope? widest = null;
        foreach (var permission in permissions)
        {
            if (permission.Resource != resource || permission.Action != action)
            {
                continue;
            }

            if (widest is null || permission.Scope > widest)
            {
                widest = permission.Scope;
            }
        }

        return widest;
    }

    private static IReadOnlyList<Permission> GrantsFor(string role)
    {
        if (role.Equals(SystemRole.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return AdminGrants;
        }

        if (role == SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Zvyazkovyi))
        {
            return StewardGrants;
        }

        if (role == SystemRole.ForOffice(LeadershipType.Kurin, LeadershipRole.Kurinnuy))
        {
            return KurinLeadGrants;
        }

        // Гуртковий leads their гурток; Впорядник/Інструктор (КВ) mentor a гурток — both get the same
        // group-scoped grants, bounded to their led groups (гуртковий office group or mentor assignment).
        if (role == SystemRole.ForOffice(LeadershipType.Group, LeadershipRole.Hurtkoviy) ||
            role == SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk) ||
            role == SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Instruktor))
        {
            return GroupLeadGrants;
        }

        // Member and every non-leading office: baseline only. Per-feature grants (e.g. Скарбник →
        // finances) attach here later without touching the enforcement code.
        return MemberGrants;
    }

    private static List<Permission> BuildMemberGrants()
    {
        var grants = new List<Permission>();
        foreach (var resource in AllResources)
        {
            grants.Add(new Permission(resource, ResourceAction.Read, AccessScope.KurinWide));
        }

        grants.Add(new Permission(ResourceType.Member, ResourceAction.Update, AccessScope.Own));
        grants.Add(new Permission(ResourceType.BadgeProgress, ResourceAction.Create, AccessScope.Own));
        grants.Add(new Permission(ResourceType.BadgeProgress, ResourceAction.Update, AccessScope.Own));
        return grants;
    }

    private static List<Permission> BuildGroupLeadGrants()
    {
        var grants = new List<Permission>(MemberGrants);
        var scopedResources = new[]
        {
            ResourceType.Group, ResourceType.Member, ResourceType.BadgeProgress, ResourceType.ProbeProgress
        };

        foreach (var resource in scopedResources)
        {
            grants.Add(new Permission(resource, ResourceAction.Create, AccessScope.OwnGroups));
            grants.Add(new Permission(resource, ResourceAction.Update, AccessScope.OwnGroups));
        }

        return grants;
    }

    private static List<Permission> BuildKurinLeadGrants()
    {
        var grants = new List<Permission>(MemberGrants);
        var managedResources = new[]
        {
            ResourceType.Group, ResourceType.Member, ResourceType.PlanningSession,
            ResourceType.BadgeProgress, ResourceType.ProbeProgress
        };
        var manageActions = new[]
        {
            ResourceAction.Create, ResourceAction.Update, ResourceAction.Delete, ResourceAction.Manage
        };

        foreach (var resource in managedResources)
        {
            foreach (var action in manageActions)
            {
                grants.Add(new Permission(resource, action, AccessScope.KurinWide));
            }
        }

        // Kurin (settings) and Leadership (office assignment) stay read-only for Курінний.
        return grants;
    }

    private static List<Permission> BuildStewardGrants()
    {
        var grants = new List<Permission>(MemberGrants);
        var managedResources = new[]
        {
            ResourceType.Group, ResourceType.Member, ResourceType.PlanningSession,
            ResourceType.BadgeProgress, ResourceType.ProbeProgress, ResourceType.Leadership
        };
        var manageActions = new[]
        {
            ResourceAction.Create, ResourceAction.Update, ResourceAction.Delete, ResourceAction.Manage
        };

        foreach (var resource in managedResources)
        {
            foreach (var action in manageActions)
            {
                grants.Add(new Permission(resource, action, AccessScope.KurinWide));
            }
        }

        // Kurin settings, but not the irreversible Delete/Manage (Admin-only).
        grants.Add(new Permission(ResourceType.Kurin, ResourceAction.Create, AccessScope.KurinWide));
        grants.Add(new Permission(ResourceType.Kurin, ResourceAction.Update, AccessScope.KurinWide));
        return grants;
    }

    private static List<Permission> BuildAdminGrants()
    {
        var grants = new List<Permission>();
        foreach (var resource in AllResources)
        {
            foreach (var action in Enum.GetValues<ResourceAction>())
            {
                grants.Add(new Permission(resource, action, AccessScope.KurinWide));
            }
        }

        return grants;
    }
}
