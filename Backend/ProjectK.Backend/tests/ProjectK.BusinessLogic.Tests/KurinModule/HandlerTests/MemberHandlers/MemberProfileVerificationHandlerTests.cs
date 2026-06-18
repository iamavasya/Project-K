using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.ProfileVerification;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using KurinEntity = ProjectK.Common.Entities.KurinModule.Kurin;
using MentorAssignmentEntity = ProjectK.Common.Entities.KurinModule.MentorAssignment;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class MemberProfileVerificationHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IMentorAssignmentRepository> _mentorAssignmentRepoMock;
        private readonly Mock<ICurrentUserContext> _currentUserContextMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly MemberProfileVerificationService _service;

        public MemberProfileVerificationHandlerTests()
        {
            _memberRepoMock = new Mock<IMemberRepository>();
            _mentorAssignmentRepoMock = new Mock<IMentorAssignmentRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.SetupGet(x => x.Members).Returns(_memberRepoMock.Object);
            _uowMock.SetupGet(x => x.MentorAssignments).Returns(_mentorAssignmentRepoMock.Object);

            _currentUserContextMock = new Mock<ICurrentUserContext>();
            _notificationServiceMock = new Mock<INotificationService>();
            _mapperMock = new Mock<IMapper>();

            _service = new MemberProfileVerificationService(
                _uowMock.Object,
                _currentUserContextMock.Object,
                _notificationServiceMock.Object,
                _mapperMock.Object);
        }

        [Fact]
        public async Task Verify_AsManagerInSameKurin_ShouldMarkVerifiedCurrent()
        {
            var actorUserKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey);

            SetupCurrentUser(actorUserKey, kurinKey, UserRole.Manager);
            SetupMember(member);
            _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new VerifyMemberProfileHandler(_service);
            var result = await handler.Handle(
                new VerifyMemberProfile(member.MemberKey, " checked "),
                CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            member.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);
            member.ProfileVerifiedAtUtc.Should().NotBeNull();
            member.ProfileVerifiedByUserKey.Should().Be(actorUserKey);
            member.ProfileVerificationNote.Should().Be("checked");
            result.Data!.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);

            _memberRepoMock.Verify(x => x.Update(member, It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Verify_LinkedMember_ShouldNotifyMemberOwner()
        {
            var actorUserKey = Guid.NewGuid();
            var memberUserKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey);
            member.UserKey = memberUserKey;

            SetupCurrentUser(actorUserKey, kurinKey, UserRole.Manager);
            SetupMember(member);
            _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _service.VerifyAsync(member.MemberKey, null, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            _notificationServiceMock.Verify(x => x.NotifyAsync(
                It.Is<NotificationRequest>(request =>
                    request.RecipientUserKey == memberUserKey
                    && request.Type == AppNotificationType.MemberProfileVerified
                    && request.Severity == AppNotificationSeverity.Success
                    && request.EntityKey == member.MemberKey
                    && request.Route == $"/member/{member.MemberKey}"
                    && request.ActorUserKey == actorUserKey
                    && request.DeduplicationKey == $"member-profile-verified:{member.MemberKey}"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Verify_WhenKurinToggleDisabled_ShouldReturnBadRequest()
        {
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey, profileVerificationEnabled: false);
            SetupCurrentUser(Guid.NewGuid(), kurinKey, UserRole.Manager);
            _memberRepoMock
                .Setup(x => x.GetByKeyAsync(member.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            var result = await _service.VerifyAsync(member.MemberKey, null, CancellationToken.None);

            result.Type.Should().Be(ResultType.BadRequest);
            result.ErrorCode.Should().Be("ProfileVerificationDisabled");
            _memberRepoMock.Verify(x => x.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Verify_WhenActorIsSameUserAsMember_ShouldReturnForbidden()
        {
            var userKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey);
            member.UserKey = userKey;

            SetupCurrentUser(userKey, kurinKey, UserRole.Manager);
            _memberRepoMock
                .Setup(x => x.GetByKeyAsync(member.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);

            var result = await _service.VerifyAsync(member.MemberKey, null, CancellationToken.None);

            result.Type.Should().Be(ResultType.Forbidden);
            _memberRepoMock.Verify(x => x.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Reset_AsManagerInSameKurin_ShouldClearVerificationFields()
        {
            var actorUserKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey);
            member.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            member.ProfileVerifiedAtUtc = DateTime.UtcNow.AddDays(-1);
            member.ProfileVerifiedByUserKey = Guid.NewGuid();
            member.ProfileVerificationNote = "existing";

            SetupCurrentUser(actorUserKey, kurinKey, UserRole.Manager);
            SetupMember(member);
            _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var handler = new ResetMemberProfileVerificationHandler(_service);
            var result = await handler.Handle(
                new ResetMemberProfileVerification(member.MemberKey),
                CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            member.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.Unverified);
            member.ProfileVerifiedAtUtc.Should().BeNull();
            member.ProfileVerifiedByUserKey.Should().BeNull();
            member.ProfileVerificationNote.Should().BeNull();
        }

        [Fact]
        public async Task Verify_AsMentorAssignedToMembersGroup_ShouldSucceed()
        {
            var mentorUserKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var groupKey = Guid.NewGuid();
            var member = CreateMember(kurinKey, groupKey: groupKey);

            SetupCurrentUser(mentorUserKey, kurinKey, UserRole.Mentor);
            SetupMember(member);
            _mentorAssignmentRepoMock
                .Setup(x => x.GetByMentorUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MentorAssignmentEntity>
                {
                    new() { MentorUserKey = mentorUserKey, GroupKey = groupKey, AssignedAtUtc = DateTime.UtcNow.AddDays(-1) }
                });
            _uowMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _service.VerifyAsync(member.MemberKey, null, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            member.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);
        }

        [Fact]
        public async Task Verify_AsMentorWithoutActiveAssignment_ShouldReturnForbidden()
        {
            var mentorUserKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var member = CreateMember(kurinKey, groupKey: Guid.NewGuid());

            SetupCurrentUser(mentorUserKey, kurinKey, UserRole.Mentor);
            _memberRepoMock
                .Setup(x => x.GetByKeyAsync(member.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mentorAssignmentRepoMock
                .Setup(x => x.GetByMentorUserKeyAsync(mentorUserKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<MentorAssignmentEntity>());

            var result = await _service.VerifyAsync(member.MemberKey, null, CancellationToken.None);

            result.Type.Should().Be(ResultType.Forbidden);
            _memberRepoMock.Verify(x => x.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        private void SetupCurrentUser(Guid userKey, Guid kurinKey, UserRole role)
        {
            _currentUserContextMock.SetupGet(x => x.UserId).Returns(userKey);
            _currentUserContextMock.SetupGet(x => x.KurinKey).Returns(kurinKey);
            _currentUserContextMock
                .Setup(x => x.IsInRole(It.IsAny<string>()))
                .Returns<string>(requestedRole => requestedRole == role.ToClaimValue());
        }

        private void SetupMember(Member member)
        {
            _memberRepoMock
                .Setup(x => x.GetByKeyAsync(member.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member);
            _mapperMock
                .Setup(x => x.Map<MemberResponse>(member))
                .Returns(() => MapMember(member));
        }

        private static Member CreateMember(
            Guid kurinKey,
            bool profileVerificationEnabled = true,
            Guid? groupKey = null)
        {
            return new Member
            {
                MemberKey = Guid.NewGuid(),
                KurinKey = kurinKey,
                GroupKey = groupKey ?? Guid.NewGuid(),
                Kurin = new KurinEntity(1)
                {
                    KurinKey = kurinKey,
                    ProfileVerificationEnabled = profileVerificationEnabled
                },
                FirstName = "Ivan",
                MiddleName = "I.",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2000, 1, 1)
            };
        }

        private static MemberResponse MapMember(Member member)
        {
            return new MemberResponse
            {
                MemberKey = member.MemberKey,
                GroupKey = member.GroupKey ?? Guid.Empty,
                KurinKey = member.KurinKey,
                FirstName = member.FirstName,
                MiddleName = member.MiddleName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                DateOfBirth = member.DateOfBirth,
                ProfileVerificationStatus = member.ProfileVerificationStatus,
                ProfileVerifiedAtUtc = member.ProfileVerifiedAtUtc,
                ProfileVerifiedByUserKey = member.ProfileVerifiedByUserKey,
                ProfileVerificationNote = member.ProfileVerificationNote
            };
        }
    }
}
