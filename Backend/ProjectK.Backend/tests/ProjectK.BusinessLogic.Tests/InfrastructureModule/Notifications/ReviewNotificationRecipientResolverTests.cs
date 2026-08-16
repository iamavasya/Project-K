using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.Notifications;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
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
        var leadershipRepositoryMock = new Mock<ILeadershipRepository>();
        var mentorAssignmentRepositoryMock = new Mock<IMentorAssignmentRepository>();
        var resolver = new ReviewNotificationRecipientResolver(unitOfWorkMock.Object);

        var kurinKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var actorUserKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();
        var mentorUserKey = Guid.NewGuid();
        var revokedMentorUserKey = Guid.NewGuid();

        var managerMemberKey = Guid.NewGuid();
        var actorMemberKey = Guid.NewGuid();
        var mentorMemberKey = Guid.NewGuid();

        unitOfWorkMock.SetupGet(x => x.Members).Returns(memberRepositoryMock.Object);
        unitOfWorkMock.SetupGet(x => x.Leaderships).Returns(leadershipRepositoryMock.Object);
        unitOfWorkMock.SetupGet(x => x.MentorAssignments).Returns(mentorAssignmentRepositoryMock.Object);

        memberRepositoryMock
            .Setup(x => x.GetMentorCandidatesLookupAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MemberLookupDto { MemberKey = managerMemberKey, UserKey = managerUserKey },
                new MemberLookupDto { MemberKey = actorMemberKey, UserKey = actorUserKey },
                new MemberLookupDto { MemberKey = mentorMemberKey, UserKey = mentorUserKey }
            });

        // Whole-kurin managers (Зв'язковий/Курінний) — both the manager and the actor hold an office.
        leadershipRepositoryMock
            .Setup(x => x.GetActiveOfficeMemberKeysAsync(It.IsAny<IReadOnlyCollection<LeadershipRole>>(), kurinKey, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { managerMemberKey, actorMemberKey });

        // Гуртковий leaders of the group.
        leadershipRepositoryMock
            .Setup(x => x.GetActiveOfficeMemberKeysAsync(It.IsAny<IReadOnlyCollection<LeadershipRole>>(), null, groupKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { mentorMemberKey });

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

        var result = await resolver.ResolveAsync(kurinKey, groupKey, actorUserKey, CancellationToken.None);

        result.Should().BeEquivalentTo(new[] { managerUserKey, mentorUserKey });
    }

    [Fact]
    public async Task ResolveAsync_ShouldReturnKurinManagers_WhenGroupIsNotSpecified()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var memberRepositoryMock = new Mock<IMemberRepository>();
        var leadershipRepositoryMock = new Mock<ILeadershipRepository>();
        var resolver = new ReviewNotificationRecipientResolver(unitOfWorkMock.Object);

        var kurinKey = Guid.NewGuid();
        var managerUserKey = Guid.NewGuid();
        var managerMemberKey = Guid.NewGuid();

        unitOfWorkMock.SetupGet(x => x.Members).Returns(memberRepositoryMock.Object);
        unitOfWorkMock.SetupGet(x => x.Leaderships).Returns(leadershipRepositoryMock.Object);

        memberRepositoryMock
            .Setup(x => x.GetMentorCandidatesLookupAsync(kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MemberLookupDto { MemberKey = managerMemberKey, UserKey = managerUserKey }
            });
        leadershipRepositoryMock
            .Setup(x => x.GetActiveOfficeMemberKeysAsync(It.IsAny<IReadOnlyCollection<LeadershipRole>>(), kurinKey, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { managerMemberKey });

        var result = await resolver.ResolveAsync(kurinKey, groupKey: null, excludedUserKey: null, CancellationToken.None);

        result.Should().ContainSingle().Which.Should().Be(managerUserKey);
        unitOfWorkMock.VerifyGet(x => x.MentorAssignments, Times.Never);
    }
}
