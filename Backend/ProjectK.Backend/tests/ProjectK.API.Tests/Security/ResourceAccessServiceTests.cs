using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.API.Tests.Security;

public class ResourceAccessServiceTests
{
    [Fact]
    public async Task UnauthenticatedUser_ShouldBeDenied()
    {
        var fixture = CreateFixture(false, Guid.NewGuid(), null, UserRole.User);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Read, Guid.NewGuid());

        Assert.False(decision.IsAllowed);
        Assert.Contains("not authenticated", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_ShouldBypassChecks()
    {
        var fixture = CreateFixture(true, null, null, UserRole.Admin);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.BadgeProgress, ResourceAction.Manage, Guid.NewGuid());

        Assert.True(decision.IsAllowed);
        Assert.Contains("Admin bypass", decision.Reason);
    }

    [Fact]
    public async Task ScopedAdmin_ShouldBeDeniedForDifferentKurinScope()
    {
        var scopedKurinKey = Guid.NewGuid();
        var otherKurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, scopedKurinKey, null, UserRole.Admin);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(otherKurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Read, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("different kurin", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScopedAdmin_ShouldBeAllowedForSameKurinScope()
    {
        var scopedKurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, scopedKurinKey, null, UserRole.Admin);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(scopedKurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Manage, memberKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Manager_ShouldBeAllowedForSameKurinScope()
    {
        var kurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, UserRole.Manager);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Delete, memberKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Manager_ShouldBeDeniedForDifferentKurinScope()
    {
        var userKurinKey = Guid.NewGuid();
        var resourceKurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, userKurinKey, null, UserRole.Manager);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(resourceKurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("different kurin", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manager_ShouldBeDeniedForIrreversibleKurinActions()
    {
        var fixture = CreateFixture(true, Guid.NewGuid(), null, UserRole.Manager);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Delete, Guid.NewGuid());

        Assert.False(decision.IsAllowed);
        Assert.Contains("irreversible kurin actions", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Manager_ShouldBeAllowedToUpdateOwnKurin()
    {
        var kurinKey = Guid.NewGuid();
        var fixture = CreateFixture(true, kurinKey, null, UserRole.Manager);
        fixture.Scope(ResourceType.Kurin, kurinKey, new ResourceScope(kurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Update, kurinKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Manager_ShouldBeAllowedToReadGroupInOwnKurin()
    {
        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var fixture = CreateFixture(true, kurinKey, null, UserRole.Manager);
        fixture.Scope(ResourceType.Group, groupKey, new ResourceScope(kurinKey, groupKey, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Group, ResourceAction.Read, groupKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Mentor_ShouldBeDeniedForGroupDeleteAction()
    {
        var fixture = CreateFixture(true, Guid.NewGuid(), Guid.NewGuid(), UserRole.Mentor);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Group, ResourceAction.Delete, Guid.NewGuid());

        Assert.False(decision.IsAllowed);
        Assert.Contains("Mentor", decision.Reason);
    }

    [Fact]
    public async Task User_ShouldBeDeniedForNonReadAction()
    {
        var fixture = CreateFixture(true, Guid.NewGuid(), Guid.NewGuid(), UserRole.User);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Group, ResourceAction.Create, Guid.NewGuid());

        Assert.False(decision.IsAllowed);
        Assert.Contains("limited to read", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task User_ShouldBeAllowedToUpdateOwnMemberProfile()
    {
        var kurinKey = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, new[] { UserRole.User }, userId);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, null, userId));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task User_ShouldBeDeniedToUpdateAnotherMemberProfile()
    {
        var kurinKey = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, new[] { UserRole.User }, userId);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, null, Guid.NewGuid()));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("only own member profile", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mentor_ShouldBeAllowedToUpdateMemberInOwnGroup()
    {
        var kurinKey = Guid.NewGuid();
        var mentorUserId = Guid.NewGuid();
        var mentorGroupKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, mentorGroupKey, new[] { UserRole.Mentor }, mentorUserId);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, mentorGroupKey, null));


        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Mentor_ShouldBeDeniedToUpdateMemberInAnotherGroup()
    {
        var kurinKey = Guid.NewGuid();
        var mentorUserId = Guid.NewGuid();
        var mentorGroupKey = Guid.NewGuid();
        var foreignGroupKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, mentorGroupKey, new[] { UserRole.Mentor }, mentorUserId);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, foreignGroupKey, null));


        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("assigned groups", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Mentor_ShouldBeAllowedToUpdateMemberInAssignedSecondaryGroup_WhenAssignmentModelIsImplemented()
    {
        var kurinKey = Guid.NewGuid();
        var mentorUserId = Guid.NewGuid();
        var mentorPrimaryGroupKey = Guid.NewGuid();
        var assignedSecondaryGroupKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, mentorPrimaryGroupKey, new[] { UserRole.Mentor }, mentorUserId);

        fixture.MentorGroups(assignedSecondaryGroupKey);

        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(kurinKey, assignedSecondaryGroupKey, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task UserReadLeadership_ShouldResolveScopeViaGroup()
    {
        var kurinKey = Guid.NewGuid();
        var leadershipKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, UserRole.User);
        fixture.Scope(ResourceType.Leadership, leadershipKey, new ResourceScope(kurinKey, groupKey, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Leadership, ResourceAction.Read, leadershipKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task MissingUserKurinScopeClaim_ShouldBeDenied()
    {
        var memberKey = Guid.NewGuid();
        var fixture = CreateFixture(true, null, null, UserRole.Manager);
        fixture.Scope(ResourceType.Member, memberKey, new ResourceScope(Guid.NewGuid(), null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Read, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("scope claim", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResourceNotFound_ShouldBeDenied()
    {
        var memberKey = Guid.NewGuid();
        var fixture = CreateFixture(true, Guid.NewGuid(), null, UserRole.Manager);
        fixture.Scope(ResourceType.Member, memberKey, null);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Read, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("not found", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProbeProgressScopeResolution_ShouldAllow_WhenMemberInSameKurin()
    {
        var kurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var probeProgressKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, UserRole.Manager);

        fixture.Scope(ResourceType.ProbeProgress, probeProgressKey, new ResourceScope(kurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.ProbeProgress, ResourceAction.Read, probeProgressKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task BadgeProgressScopeResolution_ShouldDeny_WhenMemberInForeignKurin()
    {
        var userKurinKey = Guid.NewGuid();
        var foreignKurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var badgeProgressKey = Guid.NewGuid();

        var fixture = CreateFixture(true, userKurinKey, null, UserRole.Manager);

        fixture.Scope(ResourceType.BadgeProgress, badgeProgressKey, new ResourceScope(foreignKurinKey, null, null));

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.BadgeProgress, ResourceAction.Read, badgeProgressKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("different kurin", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    private static ResourceAccessFixture CreateFixture(
        bool isAuthenticated,
        Guid? kurinKey,
        Guid? groupKey,
        params UserRole[] roles)
    {
        return CreateFixture(isAuthenticated, kurinKey, groupKey, roles, Guid.NewGuid());
    }

    private static ResourceAccessFixture CreateFixture(
        bool isAuthenticated,
        Guid? kurinKey,
        Guid? groupKey,
        UserRole[] roles,
        Guid userId)
    {
        var roleValues = roles.Select(role => role.ToClaimValue()).ToArray();

        var currentUserContext = new Mock<ICurrentUserContext>();
        currentUserContext.SetupGet(x => x.IsAuthenticated).Returns(isAuthenticated);
        currentUserContext.SetupGet(x => x.KurinKey).Returns(kurinKey);
        currentUserContext.SetupGet(x => x.UserId).Returns(userId);
        currentUserContext.SetupGet(x => x.Roles).Returns(roleValues);
        currentUserContext
            .Setup(x => x.IsInRole(It.IsAny<string>()))
            .Returns((string role) => roleValues.Contains(role, StringComparer.OrdinalIgnoreCase));

        var scopeReader = new Mock<IResourceScopeReader>();
        scopeReader
            .Setup(x => x.GetScopeAsync(It.IsAny<ResourceType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceScope?)null);

        // A mentor covers the group they belong to unless a test says otherwise.
        scopeReader
            .Setup(x => x.GetMentorGroupKeysAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(groupKey.HasValue ? new[] { groupKey.Value } : Array.Empty<Guid>());

        var service = new ResourceAccessService(scopeReader.Object, currentUserContext.Object);
        return new ResourceAccessFixture(service, scopeReader);
    }

    private sealed record ResourceAccessFixture(
        ResourceAccessService Service,
        Mock<IResourceScopeReader> ScopeReader)
    {
        public void Scope(ResourceType resourceType, Guid resourceKey, ResourceScope? scope)
        {
            ScopeReader
                .Setup(x => x.GetScopeAsync(resourceType, resourceKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(scope);
        }

        public void MentorGroups(params Guid[] groupKeys)
        {
            ScopeReader
                .Setup(x => x.GetMentorGroupKeysAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(groupKeys);
        }
    }
}
