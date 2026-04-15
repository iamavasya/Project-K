using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

public class ResourceAccessService : IResourceAccessService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUserContext;

    public ResourceAccessService(IUnitOfWork unitOfWork, ICurrentUserContext currentUserContext)
    {
        _unitOfWork = unitOfWork;
        _currentUserContext = currentUserContext;
    }

    public async Task<ResourceAccessDecision> CheckAccessAsync(
        ResourceType resourceType,
        ResourceAction action,
        Guid resourceKey,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return ResourceAccessDecision.Deny("User is not authenticated.");
        }

        if (_currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()))
        {
            return ResourceAccessDecision.Allow("Admin bypass.");
        }

        var roleActionDecision = EvaluateRoleActionPermission(resourceType, action);
        if (!roleActionDecision.IsAllowed)
        {
            return roleActionDecision;
        }

        var currentKurinKey = _currentUserContext.KurinKey;
        if (currentKurinKey is null)
        {
            return ResourceAccessDecision.Deny("Current user does not have kurin scope claim.");
        }

        var scopeResolution = await ResolveResourceKurinKeyAsync(resourceType, resourceKey, cancellationToken);
        if (!scopeResolution.IsAllowed)
        {
            return ResourceAccessDecision.Deny(scopeResolution.Reason);
        }

        var roleScopeDecision = await EvaluateRoleSpecificScopeRulesAsync(
            resourceType,
            action,
            scopeResolution,
            currentKurinKey.Value,
            cancellationToken);

        if (!roleScopeDecision.IsAllowed)
        {
            return roleScopeDecision;
        }

        if (scopeResolution.KurinKey != currentKurinKey.Value)
        {
            return ResourceAccessDecision.Deny("Resource belongs to a different kurin scope.");
        }

        return ResourceAccessDecision.Allow("Role and resource scope checks passed.");
    }

    private ResourceAccessDecision EvaluateRoleActionPermission(ResourceType resourceType, ResourceAction action)
    {
        if (_currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()))
        {
            if (resourceType == ResourceType.Kurin && action is ResourceAction.Update or ResourceAction.Delete or ResourceAction.Manage)
            {
                return ResourceAccessDecision.Deny("Manager cannot perform irreversible kurin actions.");
            }

            return ResourceAccessDecision.Allow("Manager action is allowed; validating scope.");
        }

        if (_currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
        {
            if (!IsMentorActionAllowed(resourceType, action))
            {
                return ResourceAccessDecision.Deny("Mentor role is not allowed to perform this action.");
            }

            return ResourceAccessDecision.Allow("Mentor action is allowed; validating scope.");
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue()))
        {
            if (action == ResourceAction.Read)
            {
                return ResourceAccessDecision.Allow("User read access is allowed; validating scope.");
            }

            if (resourceType == ResourceType.Member && action == ResourceAction.Update)
            {
                return ResourceAccessDecision.Allow("User may update own member profile; validating ownership.");
            }

            if (resourceType == ResourceType.BadgeProgress && action is ResourceAction.Create or ResourceAction.Update)
            {
                return ResourceAccessDecision.Allow("User may submit own badge progress; validating ownership.");
            }

            if (resourceType == ResourceType.Member && action == ResourceAction.Manage)
            {
                return ResourceAccessDecision.Deny("User role is limited to read access.");
            }

            return ResourceAccessDecision.Deny("User role is limited to read access and own member profile update.");
        }

        return ResourceAccessDecision.Deny("Current user does not have a supported role for resource access.");
    }

    private static bool IsMentorActionAllowed(ResourceType resourceType, ResourceAction action)
    {
        return resourceType switch
        {
            ResourceType.Member or ResourceType.Group =>
                action is ResourceAction.Read or ResourceAction.Create or ResourceAction.Update,

            ResourceType.Kurin or ResourceType.PlanningSession or ResourceType.Leadership =>
                action is ResourceAction.Read,

            ResourceType.ProbeProgress or ResourceType.BadgeProgress =>
                action is ResourceAction.Read or ResourceAction.Create or ResourceAction.Update,

            _ => false
        };
    }

    private async Task<ResourceAccessScopeResolution> ResolveResourceKurinKeyAsync(
        ResourceType resourceType,
        Guid resourceKey,
        CancellationToken cancellationToken)
    {
        return resourceType switch
        {
            ResourceType.Member =>
                FromMember(await _unitOfWork.Members.GetByKeyAsync(resourceKey, cancellationToken)),

            ResourceType.Group =>
                FromGroup(await _unitOfWork.Groups.GetByKeyAsync(resourceKey, cancellationToken)),

            ResourceType.Kurin =>
                FromKurin(await _unitOfWork.Kurins.GetByKeyAsync(resourceKey, cancellationToken)),

            ResourceType.PlanningSession =>
                FromPlanning(await _unitOfWork.PlanningSessions.GetByKeyAsync(resourceKey, cancellationToken)),

            ResourceType.Leadership =>
                await FromLeadershipAsync(resourceKey, cancellationToken),

            ResourceType.ProbeProgress =>
                await FromProbeProgressAsync(resourceKey, cancellationToken),

            ResourceType.BadgeProgress =>
                await FromBadgeProgressAsync(resourceKey, cancellationToken),

            _ => ResourceAccessScopeResolution.Deny("Unsupported resource type.")
        };
    }

    private static ResourceAccessScopeResolution FromMember(Member? member)
    {
        return member is null
            ? ResourceAccessScopeResolution.Deny("Member resource was not found.")
            : ResourceAccessScopeResolution.Allow(member.KurinKey, member.GroupKey, member.UserKey);
    }

    private static ResourceAccessScopeResolution FromGroup(Group? group)
    {
        return group is null
            ? ResourceAccessScopeResolution.Deny("Group resource was not found.")
            : ResourceAccessScopeResolution.Allow(group.KurinKey, group.GroupKey, null);
    }

    private static ResourceAccessScopeResolution FromKurin(Kurin? kurin)
    {
        return kurin is null
            ? ResourceAccessScopeResolution.Deny("Kurin resource was not found.")
            : ResourceAccessScopeResolution.Allow(kurin.KurinKey, null, null);
    }

    private static ResourceAccessScopeResolution FromPlanning(PlanningSession? planningSession)
    {
        return planningSession is null
            ? ResourceAccessScopeResolution.Deny("PlanningSession resource was not found.")
            : ResourceAccessScopeResolution.Allow(planningSession.KurinKey, null, null);
    }

    private async Task<ResourceAccessScopeResolution> FromLeadershipAsync(Guid leadershipKey, CancellationToken cancellationToken)
    {
        var leadership = await _unitOfWork.Leaderships.GetByKeyAsync(leadershipKey, cancellationToken);
        if (leadership is null)
        {
            return ResourceAccessScopeResolution.Deny("Leadership resource was not found.");
        }

        if (leadership.KurinKey.HasValue)
        {
            return ResourceAccessScopeResolution.Allow(leadership.KurinKey.Value, leadership.GroupKey, null);
        }

        if (!leadership.GroupKey.HasValue)
        {
            return ResourceAccessScopeResolution.Deny("Leadership resource has no kurin or group scope.");
        }

        var group = await _unitOfWork.Groups.GetByKeyAsync(leadership.GroupKey.Value, cancellationToken);
        if (group is null)
        {
            return ResourceAccessScopeResolution.Deny("Leadership group scope could not be resolved.");
        }

        return ResourceAccessScopeResolution.Allow(group.KurinKey, group.GroupKey, null);
    }

    private async Task<ResourceAccessScopeResolution> FromProbeProgressAsync(Guid probeProgressKey, CancellationToken cancellationToken)
    {
        var probeProgress = await _unitOfWork.ProbeProgresses.GetByKeyAsync(probeProgressKey, cancellationToken);
        if (probeProgress is null)
        {
            return ResourceAccessScopeResolution.Deny("ProbeProgress resource was not found.");
        }

        var member = await _unitOfWork.Members.GetByKeyAsync(probeProgress.MemberKey, cancellationToken);
        if (member is null)
        {
            return ResourceAccessScopeResolution.Deny("ProbeProgress member scope could not be resolved.");
        }

        return ResourceAccessScopeResolution.Allow(member.KurinKey, member.GroupKey, member.UserKey);
    }

    private async Task<ResourceAccessScopeResolution> FromBadgeProgressAsync(Guid badgeProgressKey, CancellationToken cancellationToken)
    {
        var badgeProgress = await _unitOfWork.BadgeProgresses.GetByKeyAsync(badgeProgressKey, cancellationToken);
        if (badgeProgress is null)
        {
            return ResourceAccessScopeResolution.Deny("BadgeProgress resource was not found.");
        }

        var member = await _unitOfWork.Members.GetByKeyAsync(badgeProgress.MemberKey, cancellationToken);
        if (member is null)
        {
            return ResourceAccessScopeResolution.Deny("BadgeProgress member scope could not be resolved.");
        }

        return ResourceAccessScopeResolution.Allow(member.KurinKey, member.GroupKey, member.UserKey);
    }

    private async Task<ResourceAccessDecision> EvaluateRoleSpecificScopeRulesAsync(
        ResourceType resourceType,
        ResourceAction action,
        ResourceAccessScopeResolution scopeResolution,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        if (_currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()))
        {
            return ResourceAccessDecision.Allow("Manager scoped checks passed.");
        }

        if (_currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
        {
            if (resourceType == ResourceType.Group)
            {
                if (action != ResourceAction.Read)
                {
                    return ResourceAccessDecision.Deny("Mentor cannot rename or delete group data.");
                }

                var mentorGroupKey = await ResolveCurrentUserGroupKeyAsync(currentKurinKey, cancellationToken);
                if (mentorGroupKey is null)
                {
                    return ResourceAccessDecision.Deny("Mentor group scope could not be resolved.");
                }

                if (scopeResolution.GroupKey != mentorGroupKey)
                {
                    return ResourceAccessDecision.Deny("Mentor has access only to own group.");
                }
            }

            if (resourceType == ResourceType.Member)
            {
                var mentorGroupKey = await ResolveCurrentUserGroupKeyAsync(currentKurinKey, cancellationToken);
                if (mentorGroupKey is null)
                {
                    return ResourceAccessDecision.Deny("Mentor group scope could not be resolved.");
                }

                if (!scopeResolution.GroupKey.HasValue || scopeResolution.GroupKey.Value != mentorGroupKey.Value)
                {
                    return ResourceAccessDecision.Deny("Mentor can manage only members from own group.");
                }
            }

            if (resourceType is ResourceType.ProbeProgress or ResourceType.BadgeProgress)
            {
                var mentorGroupKey = await ResolveCurrentUserGroupKeyAsync(currentKurinKey, cancellationToken);
                if (mentorGroupKey is null)
                {
                    return ResourceAccessDecision.Deny("Mentor group scope could not be resolved.");
                }

                if (!scopeResolution.GroupKey.HasValue || scopeResolution.GroupKey.Value != mentorGroupKey.Value)
                {
                    return ResourceAccessDecision.Deny("Mentor can manage only progress records of members from own group.");
                }
            }

            return ResourceAccessDecision.Allow("Mentor scoped checks passed.");
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue())
            && resourceType == ResourceType.Member
            && action == ResourceAction.Update)
        {
            var currentUserId = _currentUserContext.UserId;
            if (currentUserId is null)
            {
                return ResourceAccessDecision.Deny("Current user id claim is missing.");
            }

            if (!scopeResolution.MemberUserKey.HasValue || scopeResolution.MemberUserKey.Value != currentUserId.Value)
            {
                return ResourceAccessDecision.Deny("User can update only own member profile.");
            }
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue())
            && resourceType is ResourceType.BadgeProgress or ResourceType.ProbeProgress
            && action is ResourceAction.Read or ResourceAction.Create or ResourceAction.Update)
        {
            var currentUserId = _currentUserContext.UserId;
            if (currentUserId is null)
            {
                return ResourceAccessDecision.Deny("Current user id claim is missing.");
            }

            if (!scopeResolution.MemberUserKey.HasValue || scopeResolution.MemberUserKey.Value != currentUserId.Value)
            {
                return ResourceAccessDecision.Deny("User can access only own progress resources.");
            }
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

    private async Task<Guid?> ResolveCurrentUserGroupKeyAsync(Guid currentKurinKey, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return null;
        }

        var members = await _unitOfWork.Members.GetAllByKurinKeyAsync(currentKurinKey, cancellationToken);
        var currentUserMember = (members ?? Enumerable.Empty<Member>())
            .FirstOrDefault(member => member.UserKey == currentUserId.Value);
        return currentUserMember?.GroupKey;
    }

    private sealed record ResourceAccessScopeResolution(
        bool IsAllowed,
        Guid? KurinKey,
        string Reason,
        Guid? GroupKey,
        Guid? MemberUserKey)
    {
        public static ResourceAccessScopeResolution Allow(Guid kurinKey, Guid? groupKey, Guid? memberUserKey) =>
            new(true, kurinKey, "Resource scope resolved.", groupKey, memberUserKey);

        public static ResourceAccessScopeResolution Deny(string reason) =>
            new(false, null, reason, null, null);
    }
}