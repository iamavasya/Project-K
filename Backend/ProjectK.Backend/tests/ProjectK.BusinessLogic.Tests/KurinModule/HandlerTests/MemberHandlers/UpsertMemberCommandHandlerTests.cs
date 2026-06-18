using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.API.MappingProfiles.Resolvers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.Services.BlobStorageService;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class UpsertMemberHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IGroupRepository> _groupRepoMock;
        private readonly Mock<IWaitlistRepository> _waitlistRepoMock;
        private readonly Mock<IInvitationRepository> _invitationRepoMock;
        private readonly Mock<IPhotoService> _photoServiceMock;
        private readonly Mock<UserManager<AppUser>> _userManagerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<ICurrentUserContext> _currentUserContextMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly UpsertMemberHandler _handler;

        public UpsertMemberHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(t =>
                {
                    if (t == typeof(ProfilePhotoUrlResolver))
                    {
                        return new ProfilePhotoUrlResolver(new BlobStorageOptions { PublicBaseUrl = "https://cdn.test" });
                    }

                    return Activator.CreateInstance(t)!;
                });
                cfg.AddProfile(new KurinModuleProfile());
            }, loggerFactory);
            _mapper = mapperConfig.CreateMapper();

            _uowMock = new Mock<IUnitOfWork>();
            _memberRepoMock = new Mock<IMemberRepository>();
            _groupRepoMock = new Mock<IGroupRepository>();
            _waitlistRepoMock = new Mock<IWaitlistRepository>();
            _invitationRepoMock = new Mock<IInvitationRepository>();
            _photoServiceMock = new Mock<IPhotoService>();
            _emailServiceMock = new Mock<IEmailService>();
            _currentUserContextMock = new Mock<ICurrentUserContext>();
            _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());
            _notificationServiceMock = new Mock<INotificationService>();

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

            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<AppUser>())).ReturnsAsync(IdentityResult.Success);

            _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);
            _uowMock.Setup(u => u.Groups).Returns(_groupRepoMock.Object);
            _uowMock.Setup(u => u.WaitlistEntries).Returns(_waitlistRepoMock.Object);
            _uowMock.Setup(u => u.Invitations).Returns(_invitationRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _handler = new UpsertMemberHandler(
                _uowMock.Object,
                _mapper,
                _photoServiceMock.Object,
                _userManagerMock.Object,
                _emailServiceMock.Object,
                _currentUserContextMock.Object,
                _notificationServiceMock.Object);
        }

        private static Group MakeGroup(Guid? kurinKey = null)
        {
            var k = kurinKey ?? Guid.NewGuid();
            return new Group("G", k) { GroupKey = Guid.NewGuid(), Kurin = new Kurin(10) { KurinKey = k } };
        }

        private static Member MakeExistingMember(Guid? groupKey, Guid kurinKey) =>
            new()
            {
                MemberKey = Guid.NewGuid(),
                GroupKey = groupKey,
                KurinKey = kurinKey,
                FirstName = "Old",
                MiddleName = "M",
                LastName = "Name",
                Email = "old@example.com",
                PhoneNumber = "111",
                DateOfBirth = new DateOnly(1990, 1, 1),
                ProfilePhotoBlobName = "old.png"
            };

        [Fact]
        public async Task Handle_Create_NewMember_ShouldReturnCreated()
        {
            var group = MakeGroup();
            var cmd = new UpsertMember
            {
                GroupKey = group.GroupKey,
                FirstName = "Ivan",
                MiddleName = "I",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2001, 2, 3)
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(cmd.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Created);
            result.Data.Should().NotBeNull();
            result.Data!.FirstName.Should().Be("Ivan");
            result.Data.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.Unverified);
            _memberRepoMock.Verify(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Create_KurinScopedMember_ShouldReturnCreated_WithNullGroup()
        {
            var kurinKey = Guid.NewGuid();
            var cmd = new UpsertMember
            {
                KurinKey = kurinKey,
                FirstName = "Kurin",
                LastName = "Member",
                Email = "kurin.member@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2002, 2, 3)
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(cmd.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);

            Member? createdMember = null;
            _memberRepoMock.Setup(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
                .Callback<Member, CancellationToken>((m, _) => createdMember = m);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Created);
            createdMember.Should().NotBeNull();
            createdMember!.GroupKey.Should().BeNull();
            createdMember.KurinKey.Should().Be(kurinKey);
        }

        [Fact]
        public async Task Handle_Create_WithCreateUserAccount_ShouldCreateInvitationAndSendEmail()
        {
            var kurinKey = Guid.NewGuid();
            var cmd = new UpsertMember
            {
                KurinKey = kurinKey,
                CreateUserAccount = true,
                FirstName = "Olena",
                LastName = "Invite",
                Email = "olena.invite@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2003, 3, 3)
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(cmd.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);
            _waitlistRepoMock.Setup(r => r.GetByEmailAsync(cmd.Email, It.IsAny<CancellationToken>()))
                .ReturnsAsync((WaitlistEntry?)null);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Created);
            _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<AppUser>()), Times.Once);
            _waitlistRepoMock.Verify(x => x.Create(It.IsAny<WaitlistEntry>(), It.IsAny<CancellationToken>()), Times.Once);
            _invitationRepoMock.Verify(x => x.Create(It.IsAny<Invitation>(), It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(x => x.SendInvitationEmailAsync(cmd.Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _uowMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_Create_WithCreateUserAccount_WhenUserAlreadyExists_ShouldReturnConflict()
        {
            var group = MakeGroup();
            var cmd = new UpsertMember
            {
                GroupKey = group.GroupKey,
                CreateUserAccount = true,
                FirstName = "User",
                LastName = "Exists",
                Email = "exists@example.com"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(cmd.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);
            _userManagerMock.Setup(x => x.FindByEmailAsync(cmd.Email))
                .ReturnsAsync(new AppUser { Id = Guid.NewGuid(), Email = cmd.Email, UserName = cmd.Email, FirstName = "X", LastName = "Y" });

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Conflict);
            _memberRepoMock.Verify(x => x.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Update_ExistingMember_ShouldReturnSuccess_AndDeleteOldPhotoWhenChanged()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            var newBlob = new byte[] { 1, 2, 3 };
            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "NewName",
                MiddleName = "M2",
                LastName = "Surname",
                Email = "new@example.com",
                PhoneNumber = "222",
                DateOfBirth = new DateOnly(1995, 5, 5),
                BlobContent = newBlob,
                BlobFileName = "new.png",
                BlobContentType = "image/png"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            _photoServiceMock
                .Setup(p => p.UploadPhotoAsync(newBlob, "new.png", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PhotoUploadResult("new.png", "TEST_URL"));

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            _memberRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
            _photoServiceMock.Verify(p => p.DeletePhotoAsync("old.png", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Update_VerifiedCurrentMember_WhenProfileFieldChanges_ShouldMarkVerifiedStale()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            existing.ProfileVerifiedAtUtc = DateTime.UtcNow.AddDays(-1);
            existing.ProfileVerifiedByUserKey = Guid.NewGuid();

            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "Changed",
                MiddleName = existing.MiddleName ?? string.Empty,
                LastName = existing.LastName,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                DateOfBirth = existing.DateOfBirth,
                Address = existing.Address,
                School = existing.School
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedStale);
            existing.ProfileVerifiedAtUtc.Should().NotBeNull();
            existing.ProfileVerifiedByUserKey.Should().NotBeNull();
            result.Data!.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedStale);
        }

        [Fact]
        public async Task Handle_Update_LinkedVerifiedCurrentMember_WhenProfileFieldChanges_ShouldNotifyMemberOwner()
        {
            var actorUserKey = Guid.NewGuid();
            var memberUserKey = Guid.NewGuid();
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = memberUserKey;
            existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            existing.ProfileVerifiedAtUtc = DateTime.UtcNow.AddDays(-1);
            existing.ProfileVerifiedByUserKey = Guid.NewGuid();

            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "Changed",
                MiddleName = existing.MiddleName ?? string.Empty,
                LastName = existing.LastName,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                DateOfBirth = existing.DateOfBirth,
                Address = existing.Address,
                School = existing.School
            };

            _currentUserContextMock.SetupGet(x => x.UserId).Returns(actorUserKey);
            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            _notificationServiceMock.Verify(x => x.NotifyAsync(
                It.Is<NotificationRequest>(request =>
                    request.RecipientUserKey == memberUserKey
                    && request.Type == AppNotificationType.MemberProfileChangedAfterVerification
                    && request.Severity == AppNotificationSeverity.Warn
                    && request.EntityKey == existing.MemberKey
                    && request.Route == $"/member/{existing.MemberKey}"
                    && request.ActorUserKey == actorUserKey
                    && request.DeduplicationKey == $"member-profile-stale:{existing.MemberKey}"),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Update_VerifiedCurrentMember_WhenProfileDoesNotChange_ShouldKeepVerifiedCurrent()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;

            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = existing.FirstName,
                MiddleName = existing.MiddleName ?? string.Empty,
                LastName = existing.LastName,
                Email = existing.Email,
                PhoneNumber = existing.PhoneNumber,
                DateOfBirth = existing.DateOfBirth,
                Address = existing.Address,
                School = existing.School
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);
            result.Data!.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByMentor_ShouldUpdateMemberWithoutEmailConflict_AndPreserveEmail()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = Guid.NewGuid();
            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "NewName",
                MiddleName = "M2",
                LastName = "Surname",
                Email = "different@example.com",
                PhoneNumber = "222",
                DateOfBirth = new DateOnly(1995, 5, 5)
            };

            _currentUserContextMock.Setup(x => x.IsInRole(UserRole.Mentor.ToString())).Returns(true);
            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.FirstName.Should().Be("NewName");
            existing.Email.Should().Be("old@example.com");
            existing.PhoneNumber.Should().Be("222");
            _memberRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByAdmin_ShouldAllowManualEmailChange()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = Guid.NewGuid();
            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "NewName",
                MiddleName = "M2",
                LastName = "Surname",
                Email = "admin.changed@example.com",
                PhoneNumber = "222",
                DateOfBirth = new DateOnly(1995, 5, 5)
            };

            _currentUserContextMock.Setup(x => x.IsInRole(UserRole.Admin.ToString())).Returns(true);
            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.Email.Should().Be("admin.changed@example.com");
            existing.PhoneNumber.Should().Be("222");
            _memberRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByOwner_ShouldUpdateMemberWithoutEmailConflict_AndPreserveEmail()
        {
            var group = MakeGroup();
            var ownerUserKey = Guid.NewGuid();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = ownerUserKey;
            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "SelfUpdated",
                MiddleName = "M2",
                LastName = "Surname",
                Email = "different@example.com",
                PhoneNumber = "222",
                DateOfBirth = new DateOnly(1995, 5, 5)
            };

            _currentUserContextMock.SetupGet(x => x.UserId).Returns(ownerUserKey);
            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.FirstName.Should().Be("SelfUpdated");
            existing.Email.Should().Be("old@example.com");
            existing.PhoneNumber.Should().Be("222");
            _memberRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByRegularUserForAnotherMember_WhenContactInfoChanges_ShouldReturnBadRequest()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = Guid.NewGuid();
            var cmd = new UpsertMember
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "NewName",
                MiddleName = "M2",
                LastName = "Surname",
                Email = "different@example.com",
                PhoneNumber = "222",
                DateOfBirth = new DateOnly(1995, 5, 5)
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.BadRequest);
            existing.Email.Should().Be("old@example.com");
            existing.PhoneNumber.Should().Be("111");
            _memberRepoMock.Verify(r => r.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ShouldReturnInternalServerError()
        {
            var group = MakeGroup();
            var cmd = new UpsertMember
            {
                GroupKey = group.GroupKey,
                FirstName = "Ivan"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(cmd.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.InternalServerError);
        }
    }
}
