using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Authorization;

/// <summary>
/// The single source of truth for what each <see cref="SystemRole"/> may do. To grant new access,
/// change a grant list here — nothing else reasons about role names.
/// <list type="bullet">
/// <item><b>Зв'язковий (КВ)</b> — full kurin management incl. kurin settings and office assignment.</item>
/// <item><b>Виховник (КВ)</b> — runs his гурток: members, progress and the group record.</item>
/// <item><b>Курінний</b> — reads the kurin, plans, raises agenda items and assigns the курінний провід
/// below himself. He holds nothing on members: probe sign-off, awards, warnings and profile
/// verification all sit behind <c>Member:Update</c>, which he does not get.</item>
/// <item><b>Гуртковий</b> — the same, scoped to his гурток.</item>
/// <item>the rest of a провід — as their head, minus office assignment.</item>
/// <item><b>Інструктор</b> and the bare <b>Member</b> — read within the kurin plus own profile/progress.</item>
/// </list>
/// </summary>
public static class RolePermissionMap
{
    private static readonly ResourceType[] AllResources = Enum.GetValues<ResourceType>();

    // Baseline every authenticated member carries: read anything in the kurin, edit own profile,
    // submit own badge progress.
    private static readonly IReadOnlyList<Permission> MemberGrants = BuildMemberGrants();

    // Курінний провід: plan and raise agenda for the kurin, then own what they raised.
    private static readonly IReadOnlyList<Permission> KurinProvidGrants =
        BuildProvidGrants(AccessScope.KurinWide);

    // Гуртковий провід: the same, with planning bounded to their гурток.
    private static readonly IReadOnlyList<Permission> GroupProvidGrants =
        BuildProvidGrants(AccessScope.OwnGroups);

    // Курінний: провід baseline plus office assignment, bounded by AssignableOffices.
    private static readonly IReadOnlyList<Permission> KurinnyyGrants =
        Append(KurinProvidGrants, new Permission(ResourceType.Leadership, ResourceAction.Update, AccessScope.KurinWide));

    // Гуртковий: as Курінний, bounded to his гурток.
    private static readonly IReadOnlyList<Permission> HurtkovyyGrants =
        Append(GroupProvidGrants, new Permission(ResourceType.Leadership, ResourceAction.Update, AccessScope.OwnGroups));

    // Виховник: runs his гурток — members, progress and the group record itself.
    private static readonly IReadOnlyList<Permission> VykhovnykGrants = BuildVykhovnykGrants();

    // Зв'язковий: full kurin management (irreversible Kurin Delete/Manage stays Admin-only).
    private static readonly IReadOnlyList<Permission> ZvyazkovyiGrants = BuildZvyazkovyiGrants();

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

    /// <summary>
    /// The offices <paramref name="roles"/> may put people into or take them out of. Only the head of
    /// each провід assigns, and never his own office — so a Писар cannot touch the Скарбник.
    /// </summary>
    public static IReadOnlySet<(LeadershipType Type, LeadershipRole Role)> AssignableOffices(IEnumerable<string> roles)
    {
        var assignable = new HashSet<(LeadershipType, LeadershipRole)>();
        foreach (var role in roles)
        {
            if (role.Equals(SystemRole.Admin, StringComparison.OrdinalIgnoreCase)
                || role == SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Zvyazkovyi))
            {
                foreach (var office in LeadershipOffices.All())
                {
                    assignable.Add(office);
                }

                continue;
            }

            if (role == SystemRole.ForOffice(LeadershipType.Kurin, LeadershipRole.Kurinnuy))
            {
                AddBodyExceptHead(assignable, LeadershipType.Kurin, LeadershipRole.Kurinnuy);
            }
            else if (role == SystemRole.ForOffice(LeadershipType.Group, LeadershipRole.Hurtkoviy))
            {
                AddBodyExceptHead(assignable, LeadershipType.Group, LeadershipRole.Hurtkoviy);
            }
        }

        return assignable;
    }

    /// <summary>Whether the roles grant whole-kurin management (Зв'язковий/admin).</summary>
    public static bool GrantsWholeKurinManagement(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.Group, ResourceAction.Manage) == AccessScope.KurinWide;

    /// <summary>Whether the roles grant group leadership at any scope (Виховник or higher).</summary>
    public static bool GrantsGroupLeadership(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.Group, ResourceAction.Update) is not null;

    /// <summary>
    /// Whether the roles may raise agenda items. Creation has no existing resource to scope against,
    /// so it is gated as a capability here and the target kurin is checked separately.
    /// </summary>
    public static bool GrantsAgendaAuthoring(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.AgendaItem, ResourceAction.Create) is not null;

    /// <summary>Whether the roles may open a planning session. Gated as a capability, like agenda creation.</summary>
    public static bool GrantsPlanningAuthoring(IEnumerable<string> roles) =>
        WidestScope(Resolve(roles), ResourceType.PlanningSession, ResourceAction.Create) is not null;

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
            return ZvyazkovyiGrants;
        }

        if (role == SystemRole.ForOffice(LeadershipType.KV, LeadershipRole.Vykhovnyk))
        {
            return VykhovnykGrants;
        }

        if (role == SystemRole.ForOffice(LeadershipType.Kurin, LeadershipRole.Kurinnuy))
        {
            return KurinnyyGrants;
        }

        if (role == SystemRole.ForOffice(LeadershipType.Group, LeadershipRole.Hurtkoviy))
        {
            return HurtkovyyGrants;
        }

        if (IsProvidOffice(role, LeadershipType.Kurin, LeadershipRole.Kurinnuy))
        {
            return KurinProvidGrants;
        }

        if (IsProvidOffice(role, LeadershipType.Group, LeadershipRole.Hurtkoviy))
        {
            return GroupProvidGrants;
        }

        // Інструктор and every non-leading office: baseline only. Per-feature grants (e.g. Скарбник →
        // finances) attach here later without touching the enforcement code.
        return MemberGrants;
    }

    private static bool IsProvidOffice(string role, LeadershipType type, LeadershipRole head) =>
        LeadershipOffices.Grouping[type]
            .Where(office => office != head)
            .Any(office => role == SystemRole.ForOffice(type, office));

    private static void AddBodyExceptHead(
        HashSet<(LeadershipType, LeadershipRole)> assignable,
        LeadershipType type,
        LeadershipRole head)
    {
        foreach (var office in LeadershipOffices.Grouping[type].Where(office => office != head))
        {
            assignable.Add((type, office));
        }
    }

    private static IReadOnlyList<Permission> Append(IReadOnlyList<Permission> grants, Permission extra)
    {
        var combined = new List<Permission>(grants) { extra };
        return combined;
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
        grants.Add(new Permission(ResourceType.MemberAward, ResourceAction.Create, AccessScope.Own));
        grants.Add(new Permission(ResourceType.MemberAward, ResourceAction.Update, AccessScope.Own));
        grants.Add(new Permission(ResourceType.MemberAward, ResourceAction.Delete, AccessScope.Own));
        return grants;
    }

    /// <summary>
    /// What every провід member gets: plan and raise agenda items, then edit or drop the ones they
    /// authored. Agenda creation is always kurin-wide — who sees an item is decided by its assignment,
    /// not by the author's scope.
    /// </summary>
    private static List<Permission> BuildProvidGrants(AccessScope planningScope)
    {
        var grants = new List<Permission>(MemberGrants)
        {
            new(ResourceType.PlanningSession, ResourceAction.Create, planningScope),
            new(ResourceType.PlanningSession, ResourceAction.Update, AccessScope.Own),
            new(ResourceType.PlanningSession, ResourceAction.Delete, AccessScope.Own),
            new(ResourceType.AgendaItem, ResourceAction.Create, AccessScope.KurinWide),
            new(ResourceType.AgendaItem, ResourceAction.Update, AccessScope.Own),
            new(ResourceType.AgendaItem, ResourceAction.Delete, AccessScope.Own)
        };

        return grants;
    }

    private static List<Permission> BuildVykhovnykGrants()
    {
        var grants = new List<Permission>(GroupProvidGrants);
        var scopedResources = new[]
        {
            ResourceType.Group, ResourceType.Member, ResourceType.BadgeProgress, ResourceType.ProbeProgress,
            ResourceType.MemberWarning, ResourceType.MemberAward
        };

        foreach (var resource in scopedResources)
        {
            grants.Add(new Permission(resource, ResourceAction.Create, AccessScope.OwnGroups));
            grants.Add(new Permission(resource, ResourceAction.Update, AccessScope.OwnGroups));
        }

        // He moderates everything aimed at his гурток, agenda included — not just what he authored.
        grants.Add(new Permission(ResourceType.AgendaItem, ResourceAction.Update, AccessScope.OwnGroups));
        grants.Add(new Permission(ResourceType.AgendaItem, ResourceAction.Delete, AccessScope.OwnGroups));
        return grants;
    }

    private static List<Permission> BuildZvyazkovyiGrants()
    {
        var grants = new List<Permission>(MemberGrants);
        var managedResources = new[]
        {
            ResourceType.Group, ResourceType.Member, ResourceType.PlanningSession, ResourceType.AgendaItem,
            ResourceType.BadgeProgress, ResourceType.ProbeProgress, ResourceType.Leadership,
            ResourceType.MemberWarning, ResourceType.MemberAward
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
