using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Tests.InfrastructureModule.Notifications;

public class ReviewNotificationRecipientResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldReturnDistinctManagersAndActiveGroupMentorsExceptActor()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var mentorAssignmentRepositoryMock = new Mock<IMentorAssignmentRepository>();
        var resolver = new ReviewNotificationRecipientResolver(unitOfWorkMock.Object);

        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var actorUserKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var revokedMentorUserKey = Guid.NewGuid();

        unitOfWorkMock.SetupGet(x => x.Members).Returns(memberRepositoryMock.Object);
        unitOfWorkMock.SetupGet(x => x.MentorAssignments).Returns(mentorAssignmentRepositoryMock.Object);
        memberRepositoryMock
            .Setup(x => x.GetMentorCandidatesLookupAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MemberLookupDto { UserKey = managerUserKey, UserRole = UserRole.Manager.ToString() },
                new MemberLookupDto { UserKey = actorUserKey, UserRole = UserRole.Manager.ToString() },
                new MemberLookupDto { UserKey = mentorUserKey, UserRole = UserRole.Mentor.ToString() }
            });
        mentorAssignmentRepositoryMock
            .Setup(x => x.GetByGroupKeyAsync(groupKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MentorAssignment { GroupKey = groupKey, MentorUserKey = mentorUserKey },
                new MentorAssignment { GroupKey = groupKey, MentorUserKey = managerUserKey },
                new MentorAssignment
                {
                    GroupKey = groupKey,
                    MentorUserKey = revokedMentorUserKey,
                    RevokedAtUtc = DateTime.UtcNow
                }
            });

        var result = await resolver.ResolveAsync(
            kurinKey,
            groupKey,
            actorUserKey,
            CancellationToken.None);

        result.Should().BeEquivalentTo(new[] { managerUserKey, mentorUserKey });
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnKurinManagers_WhenGroupIsNotSpecified()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var resolver = new ReviewNotificationRecipientResolver(unitOfWorkMock.Object);

        var kurinKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();

        unitOfWorkMock.SetupGet(x => x.Members).Returns(memberRepositoryMock.Object);
        memberRepositoryMock
            .Setup(x => x.GetMentorCandidatesLookupAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MemberLookupDto { UserKey = managerUserKey, UserRole = UserRole.Manager.ToString() }
            });

        var result = await resolver.ResolveAsync(
            kurinKey,
            groupKey: null,
            excludedUserKey: null,
            CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(managerUserKey);
        unitOfWorkMock.VerifyGet(x => x.MentorAssignments, Times.Never);
    }
}
