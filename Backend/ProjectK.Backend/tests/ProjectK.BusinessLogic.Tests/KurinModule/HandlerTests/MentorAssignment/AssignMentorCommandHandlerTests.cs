using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.MentorAssignment;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MentorAssignment;

public class AssignMentorCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<IGroupRepository> _groupRepoMock = new();
    private readonly Mock<IMemberRepository> _memberRepoMock = new();
    private readonly Mock<IMentorAssignmentRepository> _mentorAssignmentRepoMock = new();
    private readonly Mock<UserManager<AppUser>> _userManagerMock;

    public AssignMentorCommandHandlerTests()
    {
        var userStoreMock = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            userStoreMock.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        _uowMock.SetupGet(x => x.Groups).Returns(_groupRepoMock.Object);
        _uowMock.SetupGet(x => x.Members).Returns(_memberRepoMock.Object);
        _uowMock.SetupGet(x => x.MentorAssignments).Returns(_mentorAssignmentRepoMock.Object);
        _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    [Fact]
    public async Task Handle_WhenAssignedUserHasUserRole_ShouldPromoteToMentor()
    {
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();

        var group = new Group("G", kurinKey) { GroupKey = groupKey, KurinKey = kurinKey };
        var mentorMember = new Member { MemberKey = Guid.NewGuid(), UserKey = mentorUserKey, KurinKey = kurinKey, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "1", DateOfBirth = new DateOnly(2000, 1, 1) };
        var user = new AppUser { Id = mentorUserKey, Email = "a@b.com", UserName = "a@b.com", FirstName = "A", LastName = "B" };

        _groupRepoMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _memberRepoMock.Setup(x => x.GetByUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>())).ReturnsAsync(mentorMember);
        _mentorAssignmentRepoMock.Setup(x => x.GetSpecificAssignmentAsync(mentorUserKey, groupKey, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectK.Common.Entities.KurinModule.MentorAssignment?)null);

        _userManagerMock.Setup(x => x.FindByIdAsync(mentorUserKey.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRole.User.ToString() });
        _userManagerMock.Setup(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(x => x.AddToRoleAsync(user, UserRole.Mentor.ToString())).ReturnsAsync(IdentityResult.Success);

        var handler = new AssignMentorCommandHandler(_uowMock.Object, _userManagerMock.Object);
        var result = await handler.Handle(new AssignMentorCommand(mentorUserKey, groupKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _mentorAssignmentRepoMock.Verify(x => x.Create(It.IsAny<ProjectK.Common.Entities.KurinModule.MentorAssignment>(), It.IsAny<CancellationToken>()), Times.Once);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(user, It.IsAny<IEnumerable<string>>()), Times.Once);
        _userManagerMock.Verify(x => x.AddToRoleAsync(user, UserRole.Mentor.ToString()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAssignedUserIsManager_ShouldNotChangeRoles()
    {
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();

        var group = new Group("G", kurinKey) { GroupKey = groupKey, KurinKey = kurinKey };
        var mentorMember = new Member { MemberKey = Guid.NewGuid(), UserKey = mentorUserKey, KurinKey = kurinKey, FirstName = "A", LastName = "B", Email = "a@b.com", PhoneNumber = "1", DateOfBirth = new DateOnly(2000, 1, 1) };
        var user = new AppUser { Id = mentorUserKey, Email = "a@b.com", UserName = "a@b.com", FirstName = "A", LastName = "B" };

        _groupRepoMock.Setup(x => x.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>())).ReturnsAsync(group);
        _memberRepoMock.Setup(x => x.GetByUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>())).ReturnsAsync(mentorMember);
        _mentorAssignmentRepoMock.Setup(x => x.GetSpecificAssignmentAsync(mentorUserKey, groupKey, It.IsAny<CancellationToken>())).ReturnsAsync((ProjectK.Common.Entities.KurinModule.MentorAssignment?)null);

        _userManagerMock.Setup(x => x.FindByIdAsync(mentorUserKey.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { UserRole.Manager.ToString() });

        var handler = new AssignMentorCommandHandler(_uowMock.Object, _userManagerMock.Object);
        var result = await handler.Handle(new AssignMentorCommand(mentorUserKey, groupKey), CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        _userManagerMock.Verify(x => x.RemoveFromRolesAsync(It.IsAny<AppUser>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _userManagerMock.Verify(x => x.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()), Times.Never);
    }
}
