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
    private const string MentorScopeChecksPassed = "Mentor scoped checks passed.";

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
            return EvaluateManagerActionPermission(resourceType, action);
        }

        if (_currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
        {
            return EvaluateMentorActionPermission(resourceType, action);
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue()))
        {
            return EvaluateUserActionPermission(resourceType, action);
        }

        return ResourceAccessDecision.Deny("Current user does not have a supported role for resource access.");
    }

    private static ResourceAccessDecision EvaluateManagerActionPermission(ResourceType resourceType, ResourceAction action)
    {
        if (resourceType == ResourceType.Kurin && action is ResourceAction.Update or ResourceAction.Delete or ResourceAction.Manage)
        {
            return ResourceAccessDecision.Deny("Manager cannot perform irreversible kurin actions.");
        }

        return ResourceAccessDecision.Allow("Manager action is allowed; validating scope.");
    }

    private static ResourceAccessDecision EvaluateMentorActionPermission(ResourceType resourceType, ResourceAction action)
    {
        if (!IsMentorActionAllowed(resourceType, action))
        {
            return ResourceAccessDecision.Deny("Mentor role is not allowed to perform this action.");
        }

        return ResourceAccessDecision.Allow("Mentor action is allowed; validating scope.");
    }

    private static ResourceAccessDecision EvaluateUserActionPermission(ResourceType resourceType, ResourceAction action)
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

        return resourceType == ResourceType.Member && action == ResourceAction.Manage
            ? ResourceAccessDecision.Deny("User role is limited to read access.")
            : ResourceAccessDecision.Deny("User role is limited to read access and own member profile update.");
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
            return await EvaluateMentorScopeRulesAsync(resourceType, action, scopeResolution, currentKurinKey, cancellationToken);
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue()))
        {
            return EvaluateUserScopeRules(resourceType, action, scopeResolution);
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

    private async Task<ResourceAccessDecision> EvaluateMentorScopeRulesAsync(
        ResourceType resourceType,
        ResourceAction action,
        ResourceAccessScopeResolution scopeResolution,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        if (!RequiresMentorGroupScope(resourceType))
        {
            return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
        }

        var mentorGroupKeys = await ResolveCurrentUserGroupKeysAsync(currentKurinKey, cancellationToken);
        if (!mentorGroupKeys.Any())
        {
            return ResourceAccessDecision.Deny("Mentor group scope could not be resolved or no groups assigned.");
        }

        return resourceType switch
        {
            ResourceType.Group => ValidateMentorGroupAccess(action, scopeResolution, mentorGroupKeys),
            ResourceType.Member => ValidateMentorMemberAccess(scopeResolution, mentorGroupKeys),
            ResourceType.ProbeProgress or ResourceType.BadgeProgress => ValidateMentorProgressAccess(scopeResolution, mentorGroupKeys),
            _ => ResourceAccessDecision.Allow(MentorScopeChecksPassed)
        };
    }

    private ResourceAccessDecision EvaluateUserScopeRules(
        ResourceType resourceType,
        ResourceAction action,
        ResourceAccessScopeResolution scopeResolution)
    {
        if (resourceType == ResourceType.Member && action == ResourceAction.Update)
        {
            return ValidateCurrentUserOwnership(scopeResolution, "User can update only own member profile.");
        }

        if (resourceType is ResourceType.BadgeProgress or ResourceType.ProbeProgress &&
            action is ResourceAction.Read or ResourceAction.Create or ResourceAction.Update)
        {
            return ValidateCurrentUserOwnership(scopeResolution, "User can access only own progress resources.");
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

    private static bool RequiresMentorGroupScope(ResourceType resourceType)
    {
        return resourceType == ResourceType.Group ||
               resourceType == ResourceType.Member ||
               resourceType == ResourceType.BadgeProgress ||
               resourceType == ResourceType.ProbeProgress;
    }

    private static ResourceAccessDecision ValidateMentorGroupAccess(
        ResourceAction action,
        ResourceAccessScopeResolution scopeResolution,
        IEnumerable<Guid> mentorGroupKeys)
    {
        if (action != ResourceAction.Read)
        {
            return ResourceAccessDecision.Deny("Mentor cannot rename or delete group data.");
        }

        if (!scopeResolution.GroupKey.HasValue || !mentorGroupKeys.Contains(scopeResolution.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor has access only to assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private static ResourceAccessDecision ValidateMentorMemberAccess(
        ResourceAccessScopeResolution scopeResolution,
        IEnumerable<Guid> mentorGroupKeys)
    {
        if (!scopeResolution.GroupKey.HasValue || !mentorGroupKeys.Contains(scopeResolution.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor can manage only members from assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private static ResourceAccessDecision ValidateMentorProgressAccess(
        ResourceAccessScopeResolution scopeResolution,
        IEnumerable<Guid> mentorGroupKeys)
    {
        if (!scopeResolution.GroupKey.HasValue || !mentorGroupKeys.Contains(scopeResolution.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor can manage only progress records of members from assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private ResourceAccessDecision ValidateCurrentUserOwnership(
        ResourceAccessScopeResolution scopeResolution,
        string denyMessage)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return ResourceAccessDecision.Deny("Current user id claim is missing.");
        }

        if (!scopeResolution.MemberUserKey.HasValue || scopeResolution.MemberUserKey.Value != currentUserId.Value)
        {
            return ResourceAccessDecision.Deny(denyMessage);
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

    private async Task<IEnumerable<Guid>> ResolveCurrentUserGroupKeysAsync(Guid currentKurinKey, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return Enumerable.Empty<Guid>();
        }

        var assignedGroups = new HashSet<Guid>();

        // 1. Explicit Mentor Assignments
        var assignments = await _unitOfWork.MentorAssignments.GetByMentorUserKeyAsync(currentUserId.Value, cancellationToken);
        var activeAssignments = (assignments ?? Enumerable.Empty<MentorAssignment>()).Where(a => a.RevokedAtUtc == null);
        foreach (var assignment in activeAssignments)
        {
            assignedGroups.Add(assignment.GroupKey);
        }

        // 2. Compatibility Fallback: Inferred own group
        // If the compatibility flag is enabled (here we assume it is implicitly on as per baseline), we add their own group.
        var members = await _unitOfWork.Members.GetAllByKurinKeyAsync(currentKurinKey, cancellationToken);
        var currentUserMember = (members ?? Enumerable.Empty<Member>())
            .FirstOrDefault(member => member.UserKey == currentUserId.Value);

        if (currentUserMember?.GroupKey != null)
        {
            assignedGroups.Add(currentUserMember.GroupKey.Value);
        }

        return assignedGroups;
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