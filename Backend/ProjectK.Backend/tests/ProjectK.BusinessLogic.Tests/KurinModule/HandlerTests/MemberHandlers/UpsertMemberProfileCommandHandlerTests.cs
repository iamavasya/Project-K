using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.BusinessLogic.MappingProfiles;
using ProjectK.BusinessLogic.MappingProfiles.Resolvers;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Settings;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class UpsertMemberProfileCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IGroupRepository> _groupRepoMock;
        private readonly Mock<ICurrentUserContext> _currentUserContextMock;
        private readonly UpsertMemberProfileCommandHandler _handler;

        public UpsertMemberProfileCommandHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(t => t == typeof(ProfilePhotoUrlResolver)
                    ? new ProfilePhotoUrlResolver(new BlobStorageOptions { PublicBaseUrl = "https://cdn.test" })
                    : Activator.CreateInstance(t)!);
                cfg.AddProfile(new KurinModuleProfile());
            }, loggerFactory);

            _uowMock = new Mock<IUnitOfWork>();
            _memberRepoMock = new Mock<IMemberRepository>();
            _groupRepoMock = new Mock<IGroupRepository>();
            _currentUserContextMock = new Mock<ICurrentUserContext>();
            _currentUserContextMock.SetupGet(x => x.UserId).Returns(Guid.NewGuid());

            _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);
            _uowMock.Setup(u => u.Groups).Returns(_groupRepoMock.Object);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _handler = new UpsertMemberProfileCommandHandler(
                _uowMock.Object,
                mapperConfig.CreateMapper(),
                _currentUserContextMock.Object);
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

        private void ExistingMemberIs(Member? member, Guid memberKey) =>
            _memberRepoMock.Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(member!);

        private void GroupIs(Group group) =>
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

        [Fact]
        public async Task Handle_Create_NewMember_ShouldReportCreated()
        {
            var group = MakeGroup();
            var cmd = new UpsertMemberProfileCommand
            {
                GroupKey = group.GroupKey,
                FirstName = "Ivan",
                MiddleName = "I",
                LastName = "Petrenko",
                Email = "ivan@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2001, 2, 3)
            };

            ExistingMemberIs(null, cmd.MemberKey);
            GroupIs(group);

            Member? created = null;
            _memberRepoMock.Setup(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
                .Callback<Member, CancellationToken>((m, _) => created = m);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data!.IsCreated.Should().BeTrue();
            created.Should().NotBeNull();
            created!.FirstName.Should().Be("Ivan");
            created.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.Unverified);
            _memberRepoMock.Verify(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Create_KurinScopedMember_ShouldLeaveGroupNull()
        {
            var kurinKey = Guid.NewGuid();
            var cmd = new UpsertMemberProfileCommand
            {
                KurinKey = kurinKey,
                FirstName = "Kurin",
                LastName = "Member",
                Email = "kurin.member@example.com",
                PhoneNumber = "123",
                DateOfBirth = new DateOnly(2002, 2, 3)
            };

            ExistingMemberIs(null, cmd.MemberKey);

            Member? created = null;
            _memberRepoMock.Setup(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()))
                .Callback<Member, CancellationToken>((m, _) => created = m);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            created.Should().NotBeNull();
            created!.GroupKey.Should().BeNull();
            created.KurinKey.Should().Be(kurinKey);
        }

        [Fact]
        public async Task Handle_WithoutGroupOrKurin_ShouldReturnNotFound()
        {
            var cmd = new UpsertMemberProfileCommand { FirstName = "Nowhere" };

            ExistingMemberIs(null, cmd.MemberKey);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.NotFound);
            _memberRepoMock.Verify(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Update_VerifiedCurrentMember_WhenProfileFieldChanges_ShouldMarkVerifiedStale()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;
            existing.ProfileVerifiedAtUtc = DateTime.UtcNow.AddDays(-1);
            existing.ProfileVerifiedByUserKey = Guid.NewGuid();

            var cmd = ProfileCommandFor(existing, group, firstName: "Changed");

            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data!.WasProfileVerifiedCurrent.Should().BeTrue();
            existing.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedStale);
            existing.ProfileVerifiedAtUtc.Should().NotBeNull();
            existing.ProfileVerifiedByUserKey.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_Update_VerifiedCurrentMember_WhenProfileDoesNotChange_ShouldKeepVerifiedCurrent()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.ProfileVerificationStatus = MemberProfileVerificationStatus.VerifiedCurrent;

            var cmd = ProfileCommandFor(existing, group);

            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.ProfileVerificationStatus.Should().Be(MemberProfileVerificationStatus.VerifiedCurrent);
        }

        [Fact]
        public async Task Handle_Update_ShouldReportThePhotoItIsReplacing()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            var cmd = ProfileCommandFor(existing, group, firstName: "Changed");

            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Data!.PreviousPhotoBlobName.Should().Be("old.png");
            result.Data.IsCreated.Should().BeFalse();
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByLeadership_ShouldPreserveEmail()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = Guid.NewGuid();
            var cmd = ProfileCommandFor(existing, group, firstName: "NewName", email: "different@example.com", phone: "222");

            _currentUserContextMock.Setup(x => x.IsInRole("KV.Vykhovnyk")).Returns(true);
            _currentUserContextMock.Setup(x => x.Roles).Returns(new[] { "KV.Vykhovnyk" });
            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

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
            var cmd = ProfileCommandFor(existing, group, firstName: "NewName", email: "admin.changed@example.com", phone: "222");

            _currentUserContextMock.Setup(x => x.IsInRole("Admin")).Returns(true);
            _currentUserContextMock.Setup(x => x.Roles).Returns(new[] { "Admin" });
            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.Email.Should().Be("admin.changed@example.com");
            existing.PhoneNumber.Should().Be("222");
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByOwner_ShouldPreserveEmail()
        {
            var group = MakeGroup();
            var ownerUserKey = Guid.NewGuid();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = ownerUserKey;
            var cmd = ProfileCommandFor(existing, group, firstName: "SelfUpdated", email: "different@example.com", phone: "222");

            _currentUserContextMock.SetupGet(x => x.UserId).Returns(ownerUserKey);
            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            existing.FirstName.Should().Be("SelfUpdated");
            existing.Email.Should().Be("old@example.com");
            existing.PhoneNumber.Should().Be("222");
        }

        [Fact]
        public async Task Handle_Update_LinkedMember_ByRegularUserForAnotherMember_WhenContactInfoChanges_ShouldReturnBadRequest()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            existing.UserKey = Guid.NewGuid();
            var cmd = ProfileCommandFor(existing, group, firstName: "NewName", email: "different@example.com", phone: "222");

            ExistingMemberIs(existing, existing.MemberKey);
            GroupIs(group);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.BadRequest);
            result.ErrorCode.Should().Be("ContactInfoLinked");
            existing.Email.Should().Be("old@example.com");
            existing.PhoneNumber.Should().Be("111");
            _memberRepoMock.Verify(r => r.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ShouldReturnInternalServerError()
        {
            var group = MakeGroup();
            var cmd = new UpsertMemberProfileCommand { GroupKey = group.GroupKey, FirstName = "Ivan" };

            ExistingMemberIs(null, cmd.MemberKey);
            GroupIs(group);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.InternalServerError);
        }

        private static UpsertMemberProfileCommand ProfileCommandFor(
            Member existing,
            Group group,
            string? firstName = null,
            string? email = null,
            string? phone = null) => new()
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = firstName ?? existing.FirstName,
                MiddleName = existing.MiddleName ?? string.Empty,
                LastName = existing.LastName,
                Email = email ?? existing.Email,
                PhoneNumber = phone ?? existing.PhoneNumber,
                DateOfBirth = existing.DateOfBirth,
                Address = existing.Address,
                School = existing.School
            };
    }
}
