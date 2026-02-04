using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.GroupHandlers
{
    public class UpsertGroupHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly UpsertGroupHandler _handler;

        public UpsertGroupHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var mapperConfig = new MapperConfiguration(cfg => cfg.AddProfile(new KurinModuleProfile()), loggerFactory);
            _mapper = mapperConfig.CreateMapper();

            _groupRepositoryMock = new Mock<IGroupRepository>();
            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.Groups).Returns(_groupRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Kurins).Returns(_kurinRepositoryMock.Object);

            _handler = new UpsertGroupHandler(_unitOfWorkMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_WhenCreatingNewGroup_ShouldCreateAndReturnCreated()
        {
            // Arrange
            var kurin = new Kurin(5) { KurinKey = Guid.NewGuid() };
            var name = "Alpha";
            Group savedGroup = null!;

            var command = new UpsertGroup(name, kurin.KurinKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            _kurinRepositoryMock
                .Setup(r => r.GetByKeyAsync(kurin.KurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(kurin);

            _groupRepositoryMock
                .Setup(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .Callback<Group, CancellationToken>((g, _) =>
                {
                    // emulate setting nav property (handler currently doesn't)
                    g.Kurin = kurin;
                    savedGroup = g;
                });

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Created);
            result.Data.Should().NotBeNull();
            result.Data!.Name.Should().Be(name);
            result.Data.KurinKey.Should().Be(kurin.KurinKey);
            result.Data.GroupKey.Should().Be(savedGroup.GroupKey);

            _groupRepositoryMock.Verify(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Once);
            _groupRepositoryMock.Verify(r => r.Update(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenKurinDoesNotExistOnCreate_ShouldReturnNotFound()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new UpsertGroup("Alpha", kurinKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            _kurinRepositoryMock
                .Setup(r => r.GetByKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Kurin)null!);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().BeNull();
            _groupRepositoryMock.Verify(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenUpdatingExistingGroup_ShouldUpdateAndReturnSuccess()
        {
            // Arrange
            var kurin = new Kurin(10) { KurinKey = Guid.NewGuid() };
            var groupKey = Guid.NewGuid();
            var existing = new Group("OldName", kurin.KurinKey) { GroupKey = groupKey, Kurin = kurin };
            var newName = "NewName";

            var command = new UpsertGroup(groupKey, newName);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            // (Handler will also call Kurins.GetByKeyAsync with request.KurinKey which is default Guid.Empty in update ctor)
            _kurinRepositoryMock
                .Setup(r => r.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Kurin)null!);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data!.GroupKey.Should().Be(groupKey);
            result.Data.Name.Should().Be(newName);
            existing.Name.Should().Be(newName);

            _groupRepositoryMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
            _groupRepositoryMock.Verify(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFailsOnCreate_ShouldReturnInternalServerError()
        {
            // Arrange
            var kurin = new Kurin(3) { KurinKey = Guid.NewGuid() };
            var command = new UpsertGroup("Bravo", kurin.KurinKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            _kurinRepositoryMock
                .Setup(r => r.GetByKeyAsync(kurin.KurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(kurin);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.InternalServerError);
            result.Data.Should().BeNull();
            _groupRepositoryMock.Verify(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrows_ShouldPropagate()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var command = new UpsertGroup(groupKey, "Name");
            var expected = new Exception("DB failure");

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            ex.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task Handle_WhenMappingWorks_ShouldMapCorrectly()
        {
            // Arrange
            var kurin = new Kurin(9) { KurinKey = Guid.NewGuid() };
            var command = new UpsertGroup("Echo", kurin.KurinKey);
            Group saved = null!;

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(Guid.Empty, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            _kurinRepositoryMock
                .Setup(r => r.GetByKeyAsync(kurin.KurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(kurin);

            _groupRepositoryMock
                .Setup(r => r.Create(It.IsAny<Group>(), It.IsAny<CancellationToken>()))
                .Callback<Group, CancellationToken>((g, _) =>
                {
                    g.Kurin = kurin;
                    saved = g;
                });

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Created);
            var mapped = _mapper.Map<GroupResponse>(saved);
            result.Data.Should().BeEquivalentTo(mapped);
        }
    }
}