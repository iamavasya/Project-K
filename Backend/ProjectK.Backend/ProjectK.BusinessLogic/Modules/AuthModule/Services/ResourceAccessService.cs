using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Services.Caching;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

public class ResourceAccessService : IResourceAccessService
{
    private const string MentorScopeChecksPassed = "Mentor scoped checks passed.";

    private readonly IResourceScopeReader _scopeReader;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IBackendCache _cache;

    public ResourceAccessService(IResourceScopeReader scopeReader, ICurrentUserContext currentUserContext, IBackendCache cache)
    {
        _scopeReader = scopeReader;
        _currentUserContext = currentUserContext;
        _cache = cache;
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

        // An unscoped admin is system-wide: that is the /panel view, where there is no kurin to
        // check against. Once they step into a kurin the claim is re-issued and they are held to
        // that scope like anyone else — otherwise browser history reaches other kurins' data.
        if (_currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()) && _currentUserContext.KurinKey is null)
        {
            return ResourceAccessDecision.Allow("Admin bypass: no kurin scope selected.");
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

        var scope = await _scopeReader.GetScopeAsync(resourceType, resourceKey, cancellationToken);
        if (scope is null)
        {
            return ResourceAccessDecision.Deny("Resource was not found or has no resolvable scope.");
        }

        var roleScopeDecision = await EvaluateRoleSpecificScopeRulesAsync(
            resourceType,
            action,
            scope,
            currentKurinKey.Value,
            cancellationToken);

        if (!roleScopeDecision.IsAllowed)
        {
            return roleScopeDecision;
        }

        if (scope.KurinKey != currentKurinKey.Value)
        {
            return ResourceAccessDecision.Deny("Resource belongs to a different kurin scope.");
        }

        return ResourceAccessDecision.Allow("Role and resource scope checks passed.");
    }

    private ResourceAccessDecision EvaluateRoleActionPermission(ResourceType resourceType, ResourceAction action)
    {
        // A scoped admin may do anything inside the kurin they stepped into; the scope
        // check further down is the only thing that still constrains them.
        if (_currentUserContext.IsInRole(UserRole.Admin.ToClaimValue()))
        {
            return ResourceAccessDecision.Allow("Admin action is allowed; validating scope.");
        }

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
        if (resourceType == ResourceType.Kurin && action is ResourceAction.Delete or ResourceAction.Manage)
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

    private async Task<ResourceAccessDecision> EvaluateRoleSpecificScopeRulesAsync(
        ResourceType resourceType,
        ResourceAction action,
        ResourceScope scope,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        if (_currentUserContext.IsInRole(UserRole.Manager.ToClaimValue()))
        {
            return ResourceAccessDecision.Allow("Manager scoped checks passed.");
        }

        if (_currentUserContext.IsInRole(UserRole.Mentor.ToClaimValue()))
        {
            return await EvaluateMentorScopeRulesAsync(resourceType, action, scope, currentKurinKey, cancellationToken);
        }

        if (_currentUserContext.IsInRole(UserRole.User.ToClaimValue()))
        {
            return EvaluateUserScopeRules(resourceType, action, scope);
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

    private async Task<ResourceAccessDecision> EvaluateMentorScopeRulesAsync(
        ResourceType resourceType,
        ResourceAction action,
        ResourceScope scope,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        if (!RequiresMentorGroupScope(resourceType))
        {
            return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
        }

        if (action == ResourceAction.Read)
        {
            return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
        }

        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return ResourceAccessDecision.Deny("Current user id claim is missing.");
        }

        var mentorGroupKeys = await _cache.GetOrCreateAsync(
            BackendCachePolicies.MentorScopeReads,
            $"groups:kurin:{currentKurinKey}",
            token => _scopeReader.GetMentorGroupKeysAsync(currentUserId.Value, currentKurinKey, token),
            cancellationToken,
            CacheScopeContext.From(_currentUserContext));

        if (mentorGroupKeys.Count == 0)
        {
            return ResourceAccessDecision.Deny("Mentor group scope could not be resolved or no groups assigned.");
        }

        return resourceType switch
        {
            ResourceType.Group => ValidateMentorGroupAccess(action, scope, mentorGroupKeys),
            ResourceType.Member => ValidateMentorMemberAccess(scope, mentorGroupKeys, currentUserId),
            ResourceType.ProbeProgress or ResourceType.BadgeProgress => ValidateMentorProgressAccess(scope, mentorGroupKeys),
            _ => ResourceAccessDecision.Allow(MentorScopeChecksPassed)
        };
    }

    private ResourceAccessDecision EvaluateUserScopeRules(
        ResourceType resourceType,
        ResourceAction action,
        ResourceScope scope)
    {
        if (resourceType == ResourceType.Member && action == ResourceAction.Update)
        {
            return ValidateCurrentUserOwnership(scope, "User can update only own member profile.");
        }

        if (resourceType is ResourceType.BadgeProgress or ResourceType.ProbeProgress &&
            action is ResourceAction.Read or ResourceAction.Create or ResourceAction.Update)
        {
            return ValidateCurrentUserOwnership(scope, "User can access only own progress resources.");
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
        ResourceScope scope,
        IEnumerable<Guid> mentorGroupKeys)
    {
        if (action != ResourceAction.Read && action != ResourceAction.Create)
        {
            return ResourceAccessDecision.Deny("Mentor cannot rename or delete group data.");
        }

        if (!scope.GroupKey.HasValue || !mentorGroupKeys.Contains(scope.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor has access only to assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private static ResourceAccessDecision ValidateMentorMemberAccess(
        ResourceScope scope,
        IEnumerable<Guid> mentorGroupKeys,
        Guid? currentUserId)
    {
        if (currentUserId.HasValue &&
            scope.MemberUserKey.HasValue &&
            scope.MemberUserKey.Value == currentUserId.Value)
        {
            return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
        }

        if (!scope.GroupKey.HasValue || !mentorGroupKeys.Contains(scope.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor can manage only own member profile or members from assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private static ResourceAccessDecision ValidateMentorProgressAccess(
        ResourceScope scope,
        IEnumerable<Guid> mentorGroupKeys)
    {
        if (!scope.GroupKey.HasValue || !mentorGroupKeys.Contains(scope.GroupKey.Value))
        {
            return ResourceAccessDecision.Deny("Mentor can manage only progress records of members from assigned groups.");
        }

        return ResourceAccessDecision.Allow(MentorScopeChecksPassed);
    }

    private ResourceAccessDecision ValidateCurrentUserOwnership(
        ResourceScope scope,
        string denyMessage)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return ResourceAccessDecision.Deny("Current user id claim is missing.");
        }

        if (!scope.MemberUserKey.HasValue || scope.MemberUserKey.Value != currentUserId.Value)
        {
            return ResourceAccessDecision.Deny(denyMessage);
        }

        return ResourceAccessDecision.Allow("Role scoped checks passed.");
    }

}
