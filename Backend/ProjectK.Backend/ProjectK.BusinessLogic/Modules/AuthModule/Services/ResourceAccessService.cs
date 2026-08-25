using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Services.Caching;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services;

/// <summary>
/// The single authorization decision point. It answers two orthogonal questions: <b>what</b> the
/// user may do (their <see cref="Permission"/> set, resolved from roles via <see cref="RolePermissionMap"/>)
/// and <b>where</b> it applies (the <see cref="AccessScope"/> of the widest matching permission,
/// checked against the resource's kurin/group/owner). Role names are never inspected here beyond the
/// system-admin bypass.
/// </summary>
public class ResourceAccessService : IResourceAccessService
{
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
        if (_currentUserContext.IsInRole(SystemRole.Admin) && _currentUserContext.KurinKey is null)
        {
            return ResourceAccessDecision.Allow("Admin bypass: no kurin scope selected.");
        }

        var permissions = RolePermissionMap.Resolve(_currentUserContext.Roles);
        var grantedScope = RolePermissionMap.WidestScope(permissions, resourceType, action);
        if (grantedScope is null)
        {
            return ResourceAccessDecision.Deny($"No permission for {action} on {resourceType}.");
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

        var scopeDecision = await EvaluateScopeAsync(grantedScope.Value, scope, currentKurinKey.Value, cancellationToken);
        if (!scopeDecision.IsAllowed)
        {
            return scopeDecision;
        }

        if (scope.KurinKey != currentKurinKey.Value)
        {
            return ResourceAccessDecision.Deny("Resource belongs to a different kurin scope.");
        }

        return ResourceAccessDecision.Allow("Permission and resource scope checks passed.");
    }

    private async Task<ResourceAccessDecision> EvaluateScopeAsync(
        AccessScope grantedScope,
        ResourceScope scope,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        switch (grantedScope)
        {
            case AccessScope.KurinWide:
                return ResourceAccessDecision.Allow("Kurin-wide permission; validating kurin scope.");

            case AccessScope.Own:
                return ValidateOwnership(scope);

            case AccessScope.OwnGroups:
                return await ValidateOwnGroupsAsync(scope, currentKurinKey, cancellationToken);

            default:
                return ResourceAccessDecision.Deny("Unknown access scope.");
        }
    }

    private ResourceAccessDecision ValidateOwnership(ResourceScope scope)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return ResourceAccessDecision.Deny("Current user id claim is missing.");
        }

        if (!scope.MemberUserKey.HasValue || scope.MemberUserKey.Value != currentUserId.Value)
        {
            return ResourceAccessDecision.Deny("Permission is limited to own resources.");
        }

        return ResourceAccessDecision.Allow("Ownership check passed.");
    }

    private async Task<ResourceAccessDecision> ValidateOwnGroupsAsync(
        ResourceScope scope,
        Guid currentKurinKey,
        CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserContext.UserId;
        if (currentUserId is null)
        {
            return ResourceAccessDecision.Deny("Current user id claim is missing.");
        }

        // Managing one's own record is always allowed within a group-scoped grant.
        if (scope.MemberUserKey.HasValue && scope.MemberUserKey.Value == currentUserId.Value)
        {
            return ResourceAccessDecision.Allow("Own record within group-scoped permission.");
        }

        var ledGroupKeys = await _cache.GetOrCreateAsync(
            BackendCachePolicies.MentorScopeReads,
            $"ledgroups:kurin:{currentKurinKey}",
            token => _scopeReader.GetLedGroupKeysAsync(currentUserId.Value, currentKurinKey, token),
            cancellationToken,
            CacheScopeContext.From(_currentUserContext));

        if (ledGroupKeys.Count == 0)
        {
            return ResourceAccessDecision.Deny("No groups are led by the current user.");
        }

        var reachesLedGroup =
            (scope.GroupKey.HasValue && ledGroupKeys.Contains(scope.GroupKey.Value))
            || (scope.GroupKeys is not null && scope.GroupKeys.Any(ledGroupKeys.Contains));

        if (!reachesLedGroup)
        {
            return ResourceAccessDecision.Deny("Permission is limited to led groups.");
        }

        return ResourceAccessDecision.Allow("Led-group scope check passed.");
    }
}
