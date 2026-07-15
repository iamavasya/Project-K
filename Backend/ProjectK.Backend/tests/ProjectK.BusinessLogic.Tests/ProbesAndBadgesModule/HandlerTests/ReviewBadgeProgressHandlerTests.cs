using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

public class ReviewBadgeProgressHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IBadgeProgressRepository> _badgeProgressRepositoryMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ReviewBadgeProgressHandler _handler;

    public ReviewBadgeProgressHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _badgeProgressRepositoryMock = new Mock<IBadgeProgressRepository>();
        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _notificationServiceMock = new Mock<INotificationService>();

        _unitOfWorkMock.SetupGet(x => x.Members).Returns(_memberRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.BadgeProgresses).Returns(_badgeProgressRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserContextMock
            .Setup(x => x.IsInRole(It.IsAny<string>()))
            .Returns((string role) => string.Equals(role, "mentor", StringComparison.OrdinalIgnoreCase));
        _currentUserContextMock.SetupGet(x => x.Roles).Returns(new[] { "mentor" });

        _handler = new ReviewBadgeProgressHandler(
            _unitOfWorkMock.Object,
            _currentUserContextMock.Object,
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldApproveSubmittedProgress()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-1";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Submitted);

        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: true, note: null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        Assert.Equal(BadgeProgressStatus.Confirmed, progress.Status);
        Assert.NotNull(progress.ReviewedAtUtc);
        Assert.Single(progress.AuditEvents);
        Assert.Equal(BadgeProgressStatus.Submitted, progress.AuditEvents.Single().FromStatus);
        Assert.Equal(BadgeProgressStatus.Confirmed, progress.AuditEvents.Single().ToStatus);
        Assert.Equal("Confirmed", progress.AuditEvents.Single().Action);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotifyMemberOwner_WhenSubmittedProgressIsApproved()
    {
        // Arrange
        var actorUserKey = Guid.NewGuid();
        var ownerUserKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-review-approved";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Submitted);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(actorUserKey);
        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(memberKey, ownerUserKey));

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: true, note: null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(notification =>
                notification.RecipientUserKey == ownerUserKey
                && notification.Type == AppNotificationType.MemberSkillReviewed
                && notification.Severity == AppNotificationSeverity.Success
                && notification.Title == "Вмілість зараховано"
                && notification.EntityType == "BadgeProgress"
                && notification.EntityKey == progress.BadgeProgressKey
                && notification.Route == $"/member/{memberKey}"
                && notification.ActorUserKey == actorUserKey
                && notification.DeduplicationKey == $"skill-review-result:{memberKey}:{badgeId}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveConfirmedProgress_WhenRequestedAsReject()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-2";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Confirmed);

        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: false, note: "remove");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        Assert.Equal(BadgeProgressStatus.Rejected, progress.Status);
        Assert.NotNull(progress.ReviewedAtUtc);
        Assert.Single(progress.AuditEvents);
        Assert.Equal(BadgeProgressStatus.Confirmed, progress.AuditEvents.Single().FromStatus);
        Assert.Equal(BadgeProgressStatus.Rejected, progress.AuditEvents.Single().ToStatus);
        Assert.Equal("RemovedConfirmed", progress.AuditEvents.Single().Action);
        Assert.Equal("remove", progress.AuditEvents.Single().Note);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotifyMemberOwnerWithWarn_WhenConfirmedProgressIsRemoved()
    {
        // Arrange
        var ownerUserKey = Guid.NewGuid();
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-review-removed";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Confirmed);

        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(memberKey, ownerUserKey));

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: false, note: "remove");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.Is<NotificationRequest>(notification =>
                notification.RecipientUserKey == ownerUserKey
                && notification.Type == AppNotificationType.MemberSkillReviewed
                && notification.Severity == AppNotificationSeverity.Warn
                && notification.Title == "Підтвердження вмілості скасовано"
                && notification.DeduplicationKey == $"skill-review-result:{memberKey}:{badgeId}"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSkipNotification_WhenMemberHasNoLinkedUser()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-no-user";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Submitted);

        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);
        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMember(memberKey, null));

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: false, note: "reject");

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        _notificationServiceMock.Verify(x => x.NotifyAsync(
            It.IsAny<NotificationRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenTryingToApproveAlreadyConfirmedProgress()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var badgeId = "badge-3";
        var progress = CreateProgress(memberKey, badgeId, BadgeProgressStatus.Confirmed);

        _badgeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndBadgeIdAsync(memberKey, badgeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        var request = new ReviewBadgeProgress(memberKey, badgeId, isApproved: true, note: null);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(BadgeProgressStatus.Confirmed, progress.Status);
        Assert.Empty(progress.AuditEvents);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BadgeProgress CreateProgress(Guid memberKey, string badgeId, BadgeProgressStatus status)
    {
        return new BadgeProgress
        {
            BadgeProgressKey = Guid.NewGuid(),
            MemberKey = memberKey,
            KurinKey = Guid.NewGuid(),
            BadgeId = badgeId,
            Status = status,
            SubmittedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static Member CreateMember(Guid memberKey, Guid? userKey)
    {
        return new Member
        {
            MemberKey = memberKey,
            UserKey = userKey,
            KurinKey = Guid.NewGuid(),
            FirstName = "Ivan",
            LastName = "Petrenko",
            Email = "ivan@example.com",
            PhoneNumber = "+380000000000"
        };
    }
}
