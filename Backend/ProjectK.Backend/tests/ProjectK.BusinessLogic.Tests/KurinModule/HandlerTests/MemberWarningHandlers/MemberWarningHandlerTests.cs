using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MemberWarning;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberWarningHandlers;

public class MemberWarningHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMemberRepository> _memberRepositoryMock;
    private readonly Mock<IMemberWarningRepository> _memberWarningRepositoryMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly Mock<AutoMapper.IMapper> _mapperMock;

    private readonly AssignMemberWarningHandler _assignHandler;
    private readonly CancelMemberWarningHandler _cancelHandler;

    public MemberWarningHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _memberRepositoryMock = new Mock<IMemberRepository>();
        _memberWarningRepositoryMock = new Mock<IMemberWarningRepository>();
        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _mapperMock = new Mock<AutoMapper.IMapper>();

        _unitOfWorkMock.SetupGet(x => x.Members).Returns(_memberRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(x => x.MemberWarnings).Returns(_memberWarningRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

        _assignHandler = new AssignMemberWarningHandler(_unitOfWorkMock.Object, _currentUserContextMock.Object, _mapperMock.Object);
        _cancelHandler = new CancelMemberWarningHandler(_unitOfWorkMock.Object, _currentUserContextMock.Object, _mapperMock.Object);
    }

    [Fact]
    public async Task Assign_Level1_ShouldCreateWarningWithThreeMonthsExpiry()
    {
        var memberKey = Guid.NewGuid();
        var userKey = _currentUserContextMock.Object.UserId!.Value;

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        _memberWarningRepositoryMock
            .Setup(x => x.GetActiveByMemberKeyAsync(memberKey, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        MemberWarning? created = null;
        _memberWarningRepositoryMock
            .Setup(x => x.Create(It.IsAny<MemberWarning>(), It.IsAny<CancellationToken>()))
            .Callback<MemberWarning, CancellationToken>((warning, _) => created = warning);

        var before = DateTime.UtcNow;
        var result = await _assignHandler.Handle(new AssignMemberWarning(memberKey, MemberWarningLevel.Level1), CancellationToken.None);
        var after = DateTime.UtcNow;

        result.Type.Should().Be(ResultType.Created);
        created.Should().NotBeNull();
        created!.MemberKey.Should().Be(memberKey);
        created.Level.Should().Be(MemberWarningLevel.Level1);
        created.IssuedByUserKey.Should().Be(userKey);
        created.IssuedAtUtc.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        created.ExpiresAtUtc.Should().BeOnOrAfter(before.AddMonths(3)).And.BeOnOrBefore(after.AddMonths(3));

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Assign_Level2_WithoutActiveLevel1_ShouldReturnConflict()
    {
        var memberKey = Guid.NewGuid();

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        _memberWarningRepositoryMock
            .Setup(x => x.GetActiveByMemberKeyAsync(memberKey, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _assignHandler.Handle(new AssignMemberWarning(memberKey, MemberWarningLevel.Level2), CancellationToken.None);

        result.Type.Should().Be(ResultType.Conflict);
        _memberWarningRepositoryMock.Verify(x => x.Create(It.IsAny<MemberWarning>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Assign_Level2_ShouldRevokeLevel1AndCreateLevel2()
    {
        var memberKey = Guid.NewGuid();
        var userKey = _currentUserContextMock.Object.UserId!.Value;
        var now = DateTime.UtcNow;

        _memberRepositoryMock
            .Setup(x => x.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member { MemberKey = memberKey, KurinKey = Guid.NewGuid() });

        var activeWarning = new MemberWarning
        {
            MemberWarningKey = Guid.NewGuid(),
            MemberKey = memberKey,
            Level = MemberWarningLevel.Level1,
            IssuedAtUtc = now.AddDays(-10),
            ExpiresAtUtc = now.AddDays(10),
            IssuedByUserKey = Guid.NewGuid()
        };

        _memberWarningRepositoryMock
            .Setup(x => x.GetActiveByMemberKeyAsync(memberKey, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([activeWarning]);

        MemberWarning? created = null;
        _memberWarningRepositoryMock
            .Setup(x => x.Create(It.IsAny<MemberWarning>(), It.IsAny<CancellationToken>()))
            .Callback<MemberWarning, CancellationToken>((warning, _) => created = warning);

        var result = await _assignHandler.Handle(new AssignMemberWarning(memberKey, MemberWarningLevel.Level2), CancellationToken.None);

        result.Type.Should().Be(ResultType.Created);
        activeWarning.RevokedAtUtc.Should().NotBeNull();
        activeWarning.RevokedByUserKey.Should().Be(userKey);
        created.Should().NotBeNull();
        created!.Level.Should().Be(MemberWarningLevel.Level2);
        created.MemberKey.Should().Be(memberKey);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_ShouldReturnForbidden_WhenUserIsNotAuthor()
    {
        var memberKey = Guid.NewGuid();
        var warningKey = Guid.NewGuid();

        _memberWarningRepositoryMock
            .Setup(x => x.GetByKeyAsync(warningKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberWarning
            {
                MemberWarningKey = warningKey,
                MemberKey = memberKey,
                Level = MemberWarningLevel.Level1,
                IssuedAtUtc = DateTime.UtcNow.AddDays(-5),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                IssuedByUserKey = Guid.NewGuid()
            });

        var result = await _cancelHandler.Handle(new CancelMemberWarning(memberKey, warningKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Forbidden);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_ShouldRevokeWarning_WhenUserIsAuthor()
    {
        var memberKey = Guid.NewGuid();
        var warningKey = Guid.NewGuid();
        var userKey = _currentUserContextMock.Object.UserId!.Value;

        var warning = new MemberWarning
        {
            MemberWarningKey = warningKey,
            MemberKey = memberKey,
            Level = MemberWarningLevel.Level1,
            IssuedAtUtc = DateTime.UtcNow.AddDays(-5),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
            IssuedByUserKey = userKey
        };

        _memberWarningRepositoryMock
            .Setup(x => x.GetByKeyAsync(warningKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(warning);

        var result = await _cancelHandler.Handle(new CancelMemberWarning(memberKey, warningKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        warning.RevokedAtUtc.Should().NotBeNull();
        warning.RevokedByUserKey.Should().Be(userKey);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
