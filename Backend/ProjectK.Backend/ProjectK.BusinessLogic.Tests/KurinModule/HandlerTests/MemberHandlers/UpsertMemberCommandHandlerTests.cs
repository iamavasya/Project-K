using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.API.MappingProfiles.Resolvers;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members.Handlers;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using ProjectK.Infrastructure.Services.BlobStorageService;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class UpsertMemberCommandHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly Mock<IGroupRepository> _groupRepoMock;
        private readonly Mock<IPhotoService> _photoServiceMock;
        private readonly UpsertMemberCommandHandler _handler;

        public UpsertMemberCommandHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(t =>
                {
                    if (t == typeof(ProfilePhotoUrlResolver))
                        return new ProfilePhotoUrlResolver(new BlobStorageOptions { PublicBaseUrl = "https://cdn.test" });
                    return Activator.CreateInstance(t)!;
                });
                cfg.AddProfile(new KurinModuleProfile());
            }, loggerFactory);
            _mapper = mapperConfig.CreateMapper();

            _uowMock = new Mock<IUnitOfWork>();
            _memberRepoMock = new Mock<IMemberRepository>();
            _groupRepoMock = new Mock<IGroupRepository>();
            _photoServiceMock = new Mock<IPhotoService>();

            _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);
            _uowMock.Setup(u => u.Groups).Returns(_groupRepoMock.Object);

            _handler = new UpsertMemberCommandHandler(_uowMock.Object, _mapper, _photoServiceMock.Object);
        }

        private static Group MakeGroup(Guid? kurinKey = null)
        {
            var k = kurinKey ?? Guid.NewGuid();
            return new Group("G", k) { GroupKey = Guid.NewGuid(), Kurin = new Kurin(10) { KurinKey = k } };
        }

        private static Member MakeExistingMember(Guid groupKey, Guid kurinKey) =>
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
            var cmd = new UpsertMemberCommand
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
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Created);
            result.Data.Should().NotBeNull();
            result.Data!.FirstName.Should().Be("Ivan");
            result.CreatedAtActionName.Should().Be("GetByKey");
            _memberRepoMock.Verify(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Once);
            _memberRepoMock.Verify(r => r.Update(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Create_EmptyMemberRequest_ShouldReturnNotFound()
        {
            var cmd = new UpsertMemberCommand
            {
                GroupKey = Guid.NewGuid(),
                FirstName = "Test"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Member)null!);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(cmd.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.NotFound);
            _memberRepoMock.Verify(r => r.Create(It.IsAny<Member>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_Update_ExistingMember_ShouldReturnSuccess_AndDeleteOldPhotoWhenChanged()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            var newBlob = new byte[] { 1, 2, 3 };
            var cmd = new UpsertMemberCommand
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

            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data!.FirstName.Should().Be("NewName");

            _memberRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
            _photoServiceMock.Verify(p => p.UploadPhotoAsync(newBlob, "new.png", It.IsAny<CancellationToken>()), Times.Once);
            _photoServiceMock.Verify(p => p.DeletePhotoAsync("old.png", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Update_NoNewPhoto_ShouldNotDeleteOldPhoto()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            var cmd = new UpsertMemberCommand
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                FirstName = "Changed"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);
            _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(cmd, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            _photoServiceMock.Verify(p => p.UploadPhotoAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_SaveChangesFailed_ShouldReturnInternalServerError()
        {
            var group = MakeGroup();
            var cmd = new UpsertMemberCommand
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

        [Fact]
        public async Task Handle_Update_PhotoUploadFails_ShouldNotChangeOldPhotoOrDelete()
        {
            var group = MakeGroup();
            var existing = MakeExistingMember(group.GroupKey, group.KurinKey);
            var cmd = new UpsertMemberCommand
            {
                MemberKey = existing.MemberKey,
                GroupKey = group.GroupKey,
                BlobContent = new byte[] { 10 },
                BlobFileName = "broken.png"
            };

            _memberRepoMock.Setup(r => r.GetByKeyAsync(existing.MemberKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);
            _groupRepoMock.Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            _photoServiceMock.Setup(p => p.UploadPhotoAsync(It.IsAny<byte[]>(), "broken.png", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("upload failed"));

            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(cmd, CancellationToken.None));

            // Ensure we never reached save / delete since exception thrown earlier
            _photoServiceMock.Verify(p => p.DeletePhotoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
