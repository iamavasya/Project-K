using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.BusinessLogic.Services.Caching;
using Xunit;
using MentorAssignmentEntity = ProjectK.Common.Entities.KurinModule.MentorAssignment;
using ProjectK.Common.Interfaces.Modules.AuthModule;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MentorAssignment;

public class AssignMentorCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IGroupRepository> _groupRepoMock = new();
    private readonly Mock<IMemberRepository> _memberRepoMock = new();
    private readonly Mock<IMentorAssignmentRepository> _mentorAssignmentRepoMock = new();
    private readonly Mock<IBackendCache> _cacheMock = new();
    private readonly Mock<ILeadershipRoleSyncService> _roleSyncMock = new();

    public AssignMentorCommandHandlerTests()
    {
        _uowMock.SetupGet(x => x.Groups).Returns(_groupRepoMock.Object);
        _uowMock.SetupGet(x => x.Members).Returns(_memberRepoMock.Object);
        _uowMock.SetupGet(x => x.MentorAssignments).Returns(_mentorAssignmentRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static (Group group, Member member) BuildFixture(Guid groupKey, Guid kurinKey, Guid mentorUserKey)
    {
        var group = new Group("G", kurinKey) { GroupKey = groupKey, KurinKey = kurinKey };
        var member = new Member { MemberKey = Guid.NewGuid(), UserKey = mentorUserKey, KurinKey = kurinKey, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "1", DateOfBirth = new DateOnly(2000, 1, 1) };
        return (group, member);
    }

    [Fact]
    public async Task Handle_WhenAssigned_ShouldCreateAssignmentAndSyncMemberRoles()
    {
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var (group, member) = BuildFixture(groupKey, kurinKey, mentorUserKey);

        _groupRepoMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _memberRepoMock.Setup(x => x.GetByUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _mentorAssignmentRepoMock.Setup(x => x.GetSpecificAssignmentAsync(mentorUserKey, groupKey, It.IsAny<CancellationToken>())).ReturnsAsync((MentorAssignmentEntity?)null);

        var handler = new AssignMentorCommandHandler(_uowMock.Object, _roleSyncMock.Object, _cacheMock.Object);
        var result = await handler.Handle(new AssignMentorCommand(mentorUserKey, groupKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _mentorAssignmentRepoMock.Verify(x => x.Create(It.IsAny<MentorAssignmentEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        // Access comes from the synced role (Впорядник), not manual role manipulation.
        _roleSyncMock.Verify(x => x.SyncMemberAsync(member.MemberKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActivelyAssigned_ShouldReturnConflictAndNotSync()
    {
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var (group, member) = BuildFixture(groupKey, kurinKey, mentorUserKey);

        _groupRepoMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _memberRepoMock.Setup(x => x.GetByUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _mentorAssignmentRepoMock.Setup(x => x.GetSpecificAssignmentAsync(mentorUserKey, groupKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MentorAssignmentEntity { MentorAssignmentKey = Guid.NewGuid(), MentorUserKey = mentorUserKey, GroupKey = groupKey, AssignedAtUtc = DateTime.UtcNow });

        var handler = new AssignMentorCommandHandler(_uowMock.Object, _roleSyncMock.Object, _cacheMock.Object);
        var result = await handler.Handle(new AssignMentorCommand(mentorUserKey, groupKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Conflict);
        _mentorAssignmentRepoMock.Verify(x => x.Create(It.IsAny<MentorAssignmentEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        _roleSyncMock.Verify(x => x.SyncMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
