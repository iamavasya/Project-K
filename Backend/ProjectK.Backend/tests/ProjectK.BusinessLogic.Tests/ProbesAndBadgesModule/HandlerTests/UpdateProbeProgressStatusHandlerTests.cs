using Moq;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Probe.UpdateStatus;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.ProbesAndBadgesModule.HandlerTests;

public class UpdateProbeProgressStatusHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IProbeProgressRepository> _probeProgressRepositoryMock;
    private readonly Mock<IProbePointProgressRepository> _probePointProgressRepositoryMock;
    private readonly Mock<IProbesCatalogService> _probesCatalogServiceMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly UpdateProbeProgressStatusHandler _handler;

    public UpdateProbeProgressStatusHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _probeProgressRepositoryMock = new Mock<IProbeProgressRepository>();
        _probePointProgressRepositoryMock = new Mock<IProbePointProgressRepository>();
        _probesCatalogServiceMock = new Mock<IProbesCatalogService>();
        _currentUserContextMock = new Mock<ICurrentUserContext>();

        _unitOfWorkMock.SetupGet(x => x.Members).Returns(_memberRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.ProbeProgresses).Returns(_probeProgressRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.ProbePointProgresses).Returns(_probePointProgressRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _probePointProgressRepositoryMock
            .Setup(x => x.GetByMemberAndProbeAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _probesCatalogServiceMock
            .Setup(x => x.GetGroupedProbeById(It.IsAny<string>()))
            .Returns((GroupedProbeResponse?)null);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
        _currentUserContextMock.Setup(x => x.IsInRole("Admin")).Returns(false);
        _currentUserContextMock.Setup(x => x.IsInRole("Manager")).Returns(false);
        _currentUserContextMock.Setup(x => x.IsInRole("Mentor")).Returns(true);
        _currentUserContextMock.Setup(x => x.IsInRole("User")).Returns(false);
        _currentUserContextMock.SetupGet(x => x.Roles).Returns(new[] { "Mentor" });

        _handler = new UpdateProbeProgressStatusHandler(
            _unitOfWorkMock.Object,
            _currentUserContextMock.Object,
            _probesCatalogServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCompletedProbeIsReopenedViaStatusEndpoint()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var actorUserKey = _currentUserContextMock.Object.UserId!.Value;

        var request = new UpdateProbeProgressStatus(memberKey, "probe-1", ProbeProgressStatus.InProgress, "unsign");

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        _memberRepositoryMock
            .Setup(x => x.GetByUserKeyAsync(actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        var progress = new ProbeProgress
        {
            MemberKey = memberKey,
            KurinKey = Guid.NewGuid(),
            ProbeId = "probe-1",
            Status = ProbeProgressStatus.Completed,
            CompletedAtUtc = DateTime.UtcNow.AddDays(-1),
            CompletedByUserKey = actorUserKey,
            CompletedByName = "Some Signer",
            CompletedByRole = "Mentor",
            VerifiedAtUtc = DateTime.UtcNow,
            VerifiedByUserKey = actorUserKey,
            VerifiedByName = "Verifier",
            VerifiedByRole = "Manager"
        };

        _probeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndProbeIdAsync(memberKey, "probe-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(ProbeProgressStatus.Completed, progress.Status);
        Assert.NotNull(progress.CompletedAtUtc);
        Assert.Equal(actorUserKey, progress.CompletedByUserKey);
        Assert.Equal("Some Signer", progress.CompletedByName);
        Assert.Equal("Mentor", progress.CompletedByRole);
        Assert.NotNull(progress.VerifiedAtUtc);
        Assert.Equal(actorUserKey, progress.VerifiedByUserKey);
        Assert.Equal("Verifier", progress.VerifiedByName);
        Assert.Equal("Manager", progress.VerifiedByRole);
        Assert.Empty(progress.AuditEvents);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUseMemberFullName_WhenActorIsBoundToMember()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var actorUserKey = _currentUserContextMock.Object.UserId!.Value;

        var request = new UpdateProbeProgressStatus(memberKey, "probe-1", ProbeProgressStatus.Completed, null);

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        _memberRepositoryMock
            .Setup(x => x.GetByUserKeyAsync(actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member
            {
                MemberKey = Guid.NewGuid(),
                UserKey = actorUserKey,
                FirstName = "Ivan",
                LastName = "Shevchenko"
            });

        var progress = new ProbeProgress
        {
            MemberKey = memberKey,
            KurinKey = Guid.NewGuid(),
            ProbeId = "probe-1",
            Status = ProbeProgressStatus.InProgress
        };

        _probeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndProbeIdAsync(memberKey, "probe-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Success, result.Type);
        Assert.Equal("Ivan Shevchenko", progress.CompletedByName);
        Assert.Equal("Mentor", progress.CompletedByRole);

        var lastAudit = progress.AuditEvents.Last();
        Assert.Equal("Ivan Shevchenko", lastAudit.ActorName);
        Assert.Equal("Mentor", lastAudit.ActorRole);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenClosingWithoutAllSignedPoints()
    {
        // Arrange
        var memberKey = Guid.NewGuid();
        var actorUserKey = _currentUserContextMock.Object.UserId!.Value;

        var request = new UpdateProbeProgressStatus(memberKey, "probe-1", ProbeProgressStatus.Completed, null);

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        _memberRepositoryMock
            .Setup(x => x.GetByUserKeyAsync(actorUserKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member?)null);

        _probesCatalogServiceMock
            .Setup(x => x.GetGroupedProbeById("probe-1"))
            .Returns(new GroupedProbeResponse("probe-1", "Перша проба", 2, 1, []));

        _probePointProgressRepositoryMock
            .Setup(x => x.GetByMemberAndProbeAsync(memberKey, "probe-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new ProbePointProgress
                {
                    MemberKey = memberKey,
                    KurinKey = Guid.NewGuid(),
                    ProbeId = "probe-1",
                    PointId = "point-1",
                    IsSigned = true,
                    SignedAtUtc = DateTime.UtcNow,
                    SignedByUserKey = actorUserKey,
                    SignedByName = "Mentor"
                }
            ]);

        var progress = new ProbeProgress
        {
            MemberKey = memberKey,
            KurinKey = Guid.NewGuid(),
            ProbeId = "probe-1",
            Status = ProbeProgressStatus.InProgress
        };

        _probeProgressRepositoryMock
            .Setup(x => x.GetByMemberAndProbeIdAsync(memberKey, "probe-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(progress);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.Equal(ResultType.Conflict, result.Type);
        Assert.Equal(ProbeProgressStatus.InProgress, progress.Status);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
