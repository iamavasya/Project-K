using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.MeModule.Features.Duties;

/// <summary>
/// What the person's offices in one kurin let them see to: which гуртки's вмілості, boxes and
/// точкування are theirs to act on. Worked out from the roles the way the token would be, so a
/// duty appears for exactly the people the page it leads to would let in.
/// </summary>
public sealed class DutyRights
{
    private readonly IReadOnlyCollection<Permission> _permissions;
    private readonly IReadOnlySet<Guid> _ledGroupKeys;

    public DutyRights(IEnumerable<string> roles, IReadOnlySet<Guid> ledGroupKeys)
    {
        _permissions = RolePermissionMap.Resolve(roles);
        _ledGroupKeys = ledGroupKeys;
    }

    /// <summary>Whether (resource, action) is theirs over the whole kurin.</summary>
    public bool WholeKurin(ResourceType resource, ResourceAction action) =>
        RolePermissionMap.WidestScope(_permissions, resource, action) == AccessScope.KurinWide;

    /// <summary>Whether (resource, action) is theirs for this гурток: kurin-wide, or a гурток they lead.</summary>
    public bool ForGroup(ResourceType resource, ResourceAction action, Guid? groupKey) =>
        RolePermissionMap.WidestScope(_permissions, resource, action) switch
        {
            AccessScope.KurinWide => true,
            AccessScope.OwnGroups => groupKey is { } key && _ledGroupKeys.Contains(key),
            _ => false
        };

    /// <summary>Whether (resource, action) is theirs for at least one гурток.</summary>
    public bool ForAnyGroup(ResourceType resource, ResourceAction action) =>
        WholeKurin(resource, action) || (_ledGroupKeys.Count > 0 && RolePermissionMap.WidestScope(_permissions, resource, action) == AccessScope.OwnGroups);
}
