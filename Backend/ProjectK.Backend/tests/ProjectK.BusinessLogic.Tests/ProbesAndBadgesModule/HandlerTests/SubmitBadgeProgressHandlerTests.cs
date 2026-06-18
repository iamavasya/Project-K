using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Submit;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

public class SubmitBadgeProgressHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IMemberRepository> _memberRepositoryMock = new();
    private readonly Mock<IBadgeProgressRepository> _badgeProgressRepositoryMock = new();
    private readonly Mock<IMentorAssignmentRepository> _mentorAssignmentRepositoryMock = new();
    private readonly Mock<ICurrentUserContext> _currentUserContextMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IReviewNotificationRecipientResolver> _recipientResolverMock = new();
    private readonly SubmitBadgeProgressHandler _handler;

    public SubmitBadgeProgressHandlerTests()
    {
        _unitOfWorkMock.SetupGet(x => x.Members).Returns(_memberRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.BadgeProgresses).Returns(_badgeProgressRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.MentorAssignments).Returns(_mentorAssignmentRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserContextMock.SetupGet(x => x.Roles).Returns(new[] { "mentor" });

        _handler = new SubmitBadgeProgressHandler(
            _unitOfWorkMock.Object,
            _currentUserContextMock.Object,
            _notificationServiceMock.Object,
            _recipientResolverMock.Object);
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
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member
            {
                MemberKey = memberKey,
                GroupKey = groupKey,
                KurinKey = kurinKey,
                FirstName = "Ivan",
                LastName = "Petrenko"
            });
        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BadgeProgress?)null);
        _recipientResolverMock
            .Setup(x => x.ResolveAsync(kurinKey, groupKey, actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mentorUserKey });

        var result = await _handler.Handle(new SubmitBadgeProgress(memberKey, badgeId, null), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _notificationServiceMock.Verify(x => x.NotifyManyAsync(
            It.Is<IEnumerable<NotificationRequest>>(requests =>
                requests.Count() == 1
                && requests.Single().RecipientUserKey == mentorUserKey
                && requests.Single().Type == AppNotificationType.MemberSkillSubmittedForReview
                && requests.Single().Severity == AppNotificationSeverity.Info
                && requests.Single().EntityType == "BadgeProgress"
                && requests.Single().EntityKey == memberKey
                && requests.Single().Route == $"/kurin/{kurinKey}/review/skills"
                && requests.Single().ActorUserKey == actorUserKey
                && requests.Single().DeduplicationKey == $"skill-review:{memberKey}:{badgeId}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _recipientResolverMock.Verify(x => x.ResolveAsync(
            kurinKey,
            groupKey,
            actorUserKey,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
