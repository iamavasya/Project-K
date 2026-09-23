using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit;
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
            _eventsMock.Object);
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
