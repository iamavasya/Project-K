using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;
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
    public async Task Manager_ShouldBeAllowedForSameKurinScope()
    {
        var kurinKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, UserRole.Manager);
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey });

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
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = resourceKurinKey });

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
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey, UserKey = userId });

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
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey, UserKey = Guid.NewGuid() });

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
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey, GroupKey = mentorGroupKey });

        fixture.Members
            .Setup(repo => repo.GetAllByKurinKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Member
                {
                    MemberKey = Guid.NewGuid(),
                    KurinKey = kurinKey,
                    GroupKey = mentorGroupKey,
                    UserKey = mentorUserId
                }
            ]);

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
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey, GroupKey = foreignGroupKey });

        fixture.Members
            .Setup(repo => repo.GetAllByKurinKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Member
                {
                    MemberKey = Guid.NewGuid(),
                    KurinKey = kurinKey,
                    GroupKey = mentorGroupKey,
                    UserKey = mentorUserId
                }
            ]);

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Update, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("own group", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UserReadLeadership_ShouldResolveScopeViaGroup()
    {
        var kurinKey = Guid.NewGuid();
        var leadershipKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();

        var fixture = CreateFixture(true, kurinKey, null, UserRole.User);
        fixture.Leaderships
            .Setup(repo => repo.GetByKeyAsync(leadershipKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Leadership { LeadershipKey = leadershipKey, GroupKey = groupKey });

        fixture.Groups
            .Setup(repo => repo.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Group("Test", kurinKey) { GroupKey = groupKey, KurinKey = kurinKey });

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Leadership, ResourceAction.Read, leadershipKey);

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task MissingUserKurinScopeClaim_ShouldBeDenied()
    {
        var memberKey = Guid.NewGuid();
        var fixture = CreateFixture(true, null, null, UserRole.Manager);
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        var decision = await fixture.Service.CheckAccessAsync(ResourceType.Member, ResourceAction.Read, memberKey);

        Assert.False(decision.IsAllowed);
        Assert.Contains("scope claim", decision.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResourceNotFound_ShouldBeDenied()
    {
        var memberKey = Guid.NewGuid();
        var fixture = CreateFixture(true, Guid.NewGuid(), null, UserRole.Manager);
        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

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

        fixture.ProbeProgresses
            .Setup(repo => repo.GetByKeyAsync(probeProgressKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProbeProgress { ProbeProgressKey = probeProgressKey, MemberKey = memberKey });

        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = kurinKey });

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

        fixture.BadgeProgresses
            .Setup(repo => repo.GetByKeyAsync(badgeProgressKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BadgeProgress { BadgeProgressKey = badgeProgressKey, MemberKey = memberKey });

        fixture.Members
            .Setup(repo => repo.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = foreignKurinKey });

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

        var members = new Mock<IMemberRepository>();
        var groups = new Mock<IGroupRepository>();
        var kurins = new Mock<IKurinRepository>();
        var leaderships = new Mock<ILeadershipRepository>();
        var planningSessions = new Mock<IPlanningSessionRepository>();
        var badgeProgresses = new Mock<IBadgeProgressRepository>();
        var probeProgresses = new Mock<IProbeProgressRepository>();

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(x => x.Members).Returns(members.Object);
        unitOfWork.SetupGet(x => x.Groups).Returns(groups.Object);
        unitOfWork.SetupGet(x => x.Kurins).Returns(kurins.Object);
        unitOfWork.SetupGet(x => x.Leaderships).Returns(leaderships.Object);
        unitOfWork.SetupGet(x => x.PlanningSessions).Returns(planningSessions.Object);
        unitOfWork.SetupGet(x => x.BadgeProgresses).Returns(badgeProgresses.Object);
        unitOfWork.SetupGet(x => x.ProbeProgresses).Returns(probeProgresses.Object);

        if (kurinKey.HasValue)
        {
            members
                .Setup(repo => repo.GetAllByKurinKeyAsync(kurinKey.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(
                [
                    new Member
                    {
                        MemberKey = Guid.NewGuid(),
                        KurinKey = kurinKey.Value,
                        GroupKey = groupKey,
                        UserKey = userId
                    }
                ]);
        }

        var service = new ResourceAccessService(unitOfWork.Object, currentUserContext.Object);
        return new ResourceAccessFixture(service, members, groups, kurins, leaderships, planningSessions, badgeProgresses, probeProgresses);
    }

    private sealed record ResourceAccessFixture(
        ResourceAccessService Service,
        Mock<IMemberRepository> Members,
        Mock<IGroupRepository> Groups,
        Mock<IKurinRepository> Kurins,
        Mock<ILeadershipRepository> Leaderships,
        Mock<IPlanningSessionRepository> PlanningSessions,
        Mock<IBadgeProgressRepository> BadgeProgresses,
        Mock<IProbeProgressRepository> ProbeProgresses);
}