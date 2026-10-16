using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Models.Catalog;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.MemberModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Events;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

public class SubmitBadgeProgressHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMemberDirectory> _memberDirectoryMock = new();
    private readonly Mock<IBadgeProgressRepository> _badgeProgressRepositoryMock = new();
    private readonly Mock<IMentorAssignmentRepository> _mentorAssignmentRepositoryMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserContextMock = new();
    private readonly Mock<IDomainEventPublisher> _eventsMock = new();
    private readonly Mock<IKurinCatalog> _kurinCatalogMock = new();
    private readonly SubmitBadgeProgressCommandHandler _handler;

    public SubmitBadgeProgressHandlerTests()
    {
        _unitOfWorkMock.SetupGet(x => x.BadgeProgresses).Returns(_badgeProgressRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.MentorAssignments).Returns(_mentorAssignmentRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserContextMock.SetupGet(x => x.Roles).Returns(new[] { "mentor" });

        _handler = new SubmitBadgeProgressCommandHandler(
            _unitOfWorkMock.Object,
            _memberDirectoryMock.Object,
            _currentUserContextMock.Object,
            _eventsMock.Object,
            _kurinCatalogMock.Object);

        OfferedBadges("badge-1");
    }

    private void OfferedBadges(params string[] ids)
    {
        var badges = ids.Select(id => new Badge(id, id, "", "", "", "", null, "", "", "", [], [], "", [])).ToList();
        _kurinCatalogMock
            .Setup(x => x.ForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KurinCatalogView(badges));
    }

    private Guid GivenMember(string badgeId, BadgeProgress? progress)
    {
        var memberKey = Guid.NewGuid();
        _memberDirectoryMock
            .Setup(x => x.FindAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummary(memberKey, null, Guid.NewGuid(), null, "Ivan", "Petrenko", "ivan@example.com", null));
        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        return memberKey;
    }

    [Fact]
    public async Task Handle_ShouldRefuseANewStart_OfABadgeTheKraiDoesNotOffer()
    {
        var memberKey = GivenMember("pl-badge", null);

        var result = await _handler.Handle(new SubmitBadgeProgressCommand(memberKey, "pl-badge", null), CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("BadgeNotOffered");
        _badgeProgressRepositoryMock.Verify(x => x.Create(It.IsAny<BadgeProgress>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldResubmit_ABadgeAlreadyInWork_EvenWhenNoLongerOffered()
    {
        var memberKey = GivenMember("pl-badge", new BadgeProgress { BadgeId = "pl-badge", Status = BadgeProgressStatus.Rejected });

        var result = await _handler.Handle(new SubmitBadgeProgressCommand(memberKey, "pl-badge", null), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        result.Data!.Status.Should().Be(BadgeProgressStatus.Submitted);
    }

    [Fact]
    public async Task Handle_NewSubmission_ShouldNotifyActiveGroupMentors()
    {
        var actorUserKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var badgeId = "badge-1";

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(actorUserKey);
        _memberDirectoryMock
            .Setup(x => x.FindAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberSummary(
                memberKey, null, kurinKey, groupKey, "Ivan", "Petrenko", "ivan@example.com", null));
        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BadgeProgress?)null);

        var result = await _handler.Handle(new SubmitBadgeProgressCommand(memberKey, badgeId, null), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _eventsMock.Verify(x => x.PublishAsync(
            It.Is<BadgeProgressSubmitted>(raised =>
                raised.BadgeId == badgeId
                && raised.MemberKey == memberKey
                && raised.KurinKey == kurinKey
                && raised.GroupKey == groupKey
                && raised.ActorUserKey == actorUserKey),
            It.IsAny<CancellationToken>()),
            Times.Once);

    }
}
