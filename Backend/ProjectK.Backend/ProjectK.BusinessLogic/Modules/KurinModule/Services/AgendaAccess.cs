using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Extensions;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>
/// Who the current user is on the agenda: their linked member/group, the groups whose items they may
/// see, and whether they manage the whole kurin. Built once per request from the auth claims and the
/// member record.
/// </summary>
public sealed record AgendaViewerContext(
    Guid KurinKey,
    Guid? ViewerUserKey,
    Guid? ViewerMemberKey,
    Guid? ViewerOwnGroupKey,
    IReadOnlyCollection<Guid> VisibilityGroupKeys,
    IReadOnlyCollection<Guid> ViewerLeadershipKeys,
    bool CanSeeWholeKurin,
    bool IsLeadership)
{
    public AgendaViewerScope ToScope() =>
        new(KurinKey, ViewerMemberKey, VisibilityGroupKeys, ViewerLeadershipKeys, CanSeeWholeKurin);
}

public interface IAgendaAccess
{
    /// <summary>Resolves the current user's agenda context inside <paramref name="kurinKey"/>.</summary>
    Task<AgendaViewerContext> BuildViewerAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the current user may aim an item at <paramref name="target"/>. Reuses the tested resource
    /// guard, so a mentor/group leader is bounded to assigned groups and their members while a
    /// manager/admin reaches the whole kurin.
    /// </summary>
    Task<ResourceAccessDecision> AuthorizeTargetAsync(
        AgendaTargetInput target,
        ResourceAction action,
        CancellationToken cancellationToken = default);
}

public sealed class AgendaAccess : IAgendaAccess
{
    private readonly ICurrentUserContext _currentUser;
    private readonly IUnitOfWork _uow;
    private readonly IResourceScopeReader _scopeReader;
    private readonly IResourceAccessService _resourceAccess;

    public AgendaAccess(
        ICurrentUserContext currentUser,
        IUnitOfWork uow,
        IResourceScopeReader scopeReader,
        IResourceAccessService resourceAccess)
    {
        _currentUser = currentUser;
        _uow = uow;
        _scopeReader = scopeReader;
        _resourceAccess = resourceAccess;
    }

    public async Task<AgendaViewerContext> BuildViewerAsync(Guid kurinKey, CancellationToken cancellationToken = default)
    {
        var userKey = _currentUser.UserId;

        // Whole-kurin leadership can manage groups anywhere; a гуртковий leader manages only its
        // groups. Both questions are asked through the same extensions every other handler uses.
        var canSeeWholeKurin = _currentUser.CanManageWholeKurin();
        var isLeadership = _currentUser.IsLeadership();

        Guid? memberKey = null;
        Guid? ownGroupKey = null;
        if (userKey.HasValue)
        {
            var member = await _uow.Members.GetByUserKeyAsync(userKey.Value, cancellationToken);
            if (member is not null && member.KurinKey == kurinKey)
            {
                memberKey = member.MemberKey;
                ownGroupKey = member.GroupKey;
            }
        }

        // Проводи/КВ the viewer belongs to — so an item aimed at their провід is visible to them.
        var leadershipKeys = memberKey.HasValue
            ? await _uow.Leaderships.GetActiveLeadershipKeysForMemberAsync(memberKey.Value, cancellationToken)
            : (IReadOnlyList<Guid>)Array.Empty<Guid>();

        var visibilityGroups = new HashSet<Guid>();
        if (ownGroupKey.HasValue)
        {
            visibilityGroups.Add(ownGroupKey.Value);
        }

        if (isLeadership && !canSeeWholeKurin && userKey.HasValue)
        {
            var ledGroups = await _scopeReader.GetLedGroupKeysAsync(userKey.Value, kurinKey, cancellationToken);
            foreach (var groupKey in ledGroups)
            {
                visibilityGroups.Add(groupKey);
            }
        }

        return new AgendaViewerContext(
            KurinKey: kurinKey,
            ViewerUserKey: userKey,
            ViewerMemberKey: memberKey,
            ViewerOwnGroupKey: ownGroupKey,
            VisibilityGroupKeys: visibilityGroups,
            ViewerLeadershipKeys: leadershipKeys,
            CanSeeWholeKurin: canSeeWholeKurin,
            IsLeadership: isLeadership);
    }

    public async Task<ResourceAccessDecision> AuthorizeTargetAsync(
        AgendaTargetInput target,
        ResourceAction action,
        CancellationToken cancellationToken = default)
    {
        // Aiming at a провід is authorized like aiming at its scope: a гуртковий провід as its group,
        // a курінний провід / КВ as the whole kurin. This reuses the tested resource guard.
        if (target.TargetType == AgendaTargetType.Leadership)
        {
            var leadership = await _uow.Leaderships.GetByKeyAsync(target.TargetKey, cancellationToken);
            if (leadership is null)
            {
                return ResourceAccessDecision.Deny("Leadership target was not found.");
            }

            return leadership.Type == LeadershipType.Group && leadership.GroupKey.HasValue
                ? await _resourceAccess.CheckAccessAsync(ResourceType.Group, action, leadership.GroupKey.Value, cancellationToken)
                : await _resourceAccess.CheckAccessAsync(ResourceType.Kurin, action, leadership.KurinKey ?? Guid.Empty, cancellationToken);
        }

        var resourceType = target.TargetType switch
        {
            AgendaTargetType.Kurin => ResourceType.Kurin,
            AgendaTargetType.Group => ResourceType.Group,
            AgendaTargetType.Member => ResourceType.Member,
            _ => ResourceType.Kurin
        };

        return await _resourceAccess.CheckAccessAsync(resourceType, action, target.TargetKey, cancellationToken);
    }
}

/// <summary>
/// Per-item permission checks shared by the update, delete and status handlers. Kept as pure functions
/// over an already-loaded item so no extra queries run.
/// </summary>
public static class AgendaPermissions
{
    /// <summary>Creator or leadership may edit/delete an item.</summary>
    public static bool CanManage(AgendaItem item, AgendaViewerContext viewer)
    {
        if (viewer.CanSeeWholeKurin)
        {
            return true;
        }

        if (viewer.ViewerUserKey.HasValue && item.CreatedByUserKey == viewer.ViewerUserKey.Value)
        {
            return true;
        }

        if (viewer.IsLeadership)
        {
            return item.Assignments.Any(a =>
                a.TargetType == AgendaTargetType.Group && viewer.VisibilityGroupKeys.Contains(a.TargetKey));
        }

        return false;
    }

    /// <summary>
    /// Whether the item is visible to the viewer — mirrors the repository's feed filter so an RSVP is
    /// only accepted on events the user could actually see. Requires <see cref="AgendaItem.Assignments"/>.
    /// </summary>
    public static bool IsVisibleTo(AgendaItem item, AgendaViewerContext viewer)
        => AgendaVisibility.IsVisible(item, viewer.ToScope());

    /// <summary>The current user is individually on the hook for the item (their own task).</summary>
    public static bool IsAssignee(AgendaItem item, AgendaViewerContext viewer)
    {
        return item.Assignments.Any(a =>
            (a.TargetType == AgendaTargetType.Member && viewer.ViewerMemberKey.HasValue && a.TargetKey == viewer.ViewerMemberKey.Value) ||
            (a.TargetType == AgendaTargetType.Group && viewer.ViewerOwnGroupKey.HasValue && a.TargetKey == viewer.ViewerOwnGroupKey.Value));
    }

    /// <summary>Assignees move their own tasks; creator and leadership move any in their zone.</summary>
    public static bool CanChangeStatus(AgendaItem item, AgendaViewerContext viewer)
    {
        return CanManage(item, viewer) || IsAssignee(item, viewer);
    }
}
