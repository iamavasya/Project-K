using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Badge.Review;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

public class ReviewBadgeProgressHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IBadgeProgressRepository> _badgeProgressRepositoryMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly ReviewBadgeProgressHandler _handler;

    public ReviewBadgeProgressHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _badgeProgressRepositoryMock = new Mock<IBadgeProgressRepository>();
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        _unitOfWorkMock.SetupGet(x => x.BadgeProgresses).Returns(_badgeProgressRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserContextMock
            .Setup(x => x.IsInRole(It.IsAny<string>()))
            .Returns((string role) => string.Equals(role, "mentor", StringComparison.OrdinalIgnoreCase));
        _currentUserContextMock.SetupGet(x => x.Roles).Returns(new[] { "mentor" });

        _handler = new ReviewBadgeProgressHandler(_unitOfWorkMock.Object, _currentUserContextMock.Object);
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
}
