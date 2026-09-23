using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers;

public class AgendaAccessAuthorizeTargetTests
{
    private readonly Mock<ICurrentUserContext> _currentUser = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IMemberDirectory> _memberDirectory = new();
    private readonly Mock<IResourceScopeReader> _scopeReader = new();
    private readonly Mock<IResourceAccessService> _resourceAccess = new();
    private readonly Mock<ILeadershipRepository> _leaderships = new();
    private readonly Mock<IGroupRepository> _groups = new();
    private readonly AgendaAccess _access;

    private readonly Guid _kurinKey = Guid.NewGuid();
    private readonly Guid _groupKey = Guid.NewGuid();
    private readonly Guid _otherGroupKey = Guid.NewGuid();
    private readonly Guid _userKey = Guid.NewGuid();

    public AgendaAccessAuthorizeTargetTests()
    {
        _uow.Setup(u => u.Leaderships).Returns(_leaderships.Object);
        _uow.Setup(u => u.Groups).Returns(_groups.Object);
        _groups.Setup(r => r.GetAllAsync(_kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Group> { new("Соколи", _kurinKey) { GroupKey = _groupKey }, new("Ведмеді", _kurinKey) { GroupKey = _otherGroupKey } });
        _resourceAccess.Setup(a => a.CheckAccessAsync(It.IsAny<ResourceType>(), It.IsAny<ResourceAction>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResourceAccessDecision.Deny("No permission for Create on Group."));
        _access = new AgendaAccess(_currentUser.Object, _uow.Object, _memberDirectory.Object, _scopeReader.Object, _resourceAccess.Object);
    }

    private AgendaTargetInput LeadershipTarget(Guid key) => new() { TargetType = AgendaTargetType.Leadership, TargetKey = key };
    private static AgendaTargetInput Target(AgendaTargetType type, Guid key) => new() { TargetType = type, TargetKey = key };

    /// <summary>A signed-in провід member of this kurin, seated in <paramref name="ownGroupKey"/>, holding <paramref name="role"/>.</summary>
    private void SignInAs(string role, Guid? ownGroupKey)
    {
        _currentUser.Setup(u => u.UserId).Returns(_userKey);
        _currentUser.Setup(u => u.Roles).Returns(new[] { role });
        _memberDirectory.Setup(d => d.FindByAccountAsync(_userKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummary(Guid.NewGuid(), _userKey, _kurinKey, ownGroupKey, "Марта", "Коваль", "marta@example.com", null));
    }

    // The resource guard says no to a Гуртковий (he does not run the group), yet his office covers
    // it: the гурток and its people are his to address, the neighbouring гурток is not.
    [Fact]
    public async Task Authorize_GroupProvid_MayAimAtOwnGroup_NotAtAnother()
    {
        SignInAs(SystemRole.ForOffice(LeadershipType.Group, LeadershipRole.Hurtkoviy), _groupKey);
        _memberDirectory.Setup(d => d.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummary(Guid.NewGuid(), null, _kurinKey, _otherGroupKey, "Ігор", "Бондар", "ihor@example.com", null));

        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Group, _groupKey), ResourceAction.Create)).IsAllowed.Should().BeTrue();
        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Group, _otherGroupKey), ResourceAction.Create)).IsAllowed.Should().BeFalse();
        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Member, Guid.NewGuid()), ResourceAction.Create)).IsAllowed.Should().BeFalse();
        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Kurin, _kurinKey), ResourceAction.Create)).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task Authorize_KurinProvid_MayAimAtTheWholeKurin()
    {
        SignInAs(SystemRole.ForOffice(LeadershipType.Kurin, LeadershipRole.Kurinnuy), null);

        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Kurin, _kurinKey), ResourceAction.Create)).IsAllowed.Should().BeTrue();
        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Group, _otherGroupKey), ResourceAction.Create)).IsAllowed.Should().BeTrue();
        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Kurin, Guid.NewGuid()), ResourceAction.Create)).IsAllowed.Should().BeFalse();
    }

    // The провід rule is about addressing, not editing: an Update on the target still needs the real right.
    [Fact]
    public async Task Authorize_ProvidRule_DoesNotCoverUpdate()
    {
        SignInAs(SystemRole.ForOffice(LeadershipType.Group, LeadershipRole.Hurtkoviy), _groupKey);

        (await _access.AuthorizeTargetAsync(Target(AgendaTargetType.Group, _groupKey), ResourceAction.Update)).IsAllowed.Should().BeFalse();
    }

    [Fact]
    public async Task Authorize_GroupOffice_ChecksAccessOnItsGroup()
    {
        var leadership = new Leadership { Type = LeadershipType.Group, GroupKey = _groupKey, KurinKey = _kurinKey };
        _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(leadership);
        _resourceAccess.Setup(a => a.CheckAccessAsync(ResourceType.Group, ResourceAction.Create, _groupKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResourceAccessDecision.Allow("ok"));

        var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

        result.IsAllowed.Should().BeTrue();
        _resourceAccess.Verify(a => a.CheckAccessAsync(ResourceType.Group, ResourceAction.Create, _groupKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Authorize_KurinOrKvOffice_ChecksAccessOnTheKurin()
    {
        var leadership = new Leadership { Type = LeadershipType.KV, GroupKey = null, KurinKey = _kurinKey };
        _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(leadership);
        _resourceAccess.Setup(a => a.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Create, _kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResourceAccessDecision.Allow("ok"));

        var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

        result.IsAllowed.Should().BeTrue();
        _resourceAccess.Verify(a => a.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Create, _kurinKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Authorize_MissingLeadership_Denies()
    {
        _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Leadership?)null);

        var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

        result.IsAllowed.Should().BeFalse();
        _resourceAccess.Verify(a => a.CheckAccessAsync(It.IsAny<ResourceType>(), It.IsAny<ResourceAction>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
