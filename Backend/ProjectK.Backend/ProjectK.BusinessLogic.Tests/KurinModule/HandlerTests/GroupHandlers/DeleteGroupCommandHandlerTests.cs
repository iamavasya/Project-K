using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups.Handlers;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.GroupHandlers
{
    public class DeleteGroupCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly DeleteGroupCommandHandler _handler;

        public DeleteGroupCommandHandlerTests()
        {
            _groupRepositoryMock = new Mock<IGroupRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.Groups).Returns(_groupRepositoryMock.Object);

            _handler = new DeleteGroupCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenGroupExists_ShouldDeleteGroupAndReturnSuccess()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var group = new Group("Alpha", Guid.NewGuid()) { GroupKey = groupKey };
            var command = new DeleteGroupCommand(groupKey);

            _groupRepositoryMock.Setup(r => r.GetByKeyAsync(groupKey, default))
                .ReturnsAsync(group);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().BeNull();
            _groupRepositoryMock.Verify(r => r.Delete(group, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenGroupDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var command = new DeleteGroupCommand(groupKey);

            _groupRepositoryMock.Setup(r => r.GetByKeyAsync(groupKey, default))
                .ReturnsAsync((Group)null!);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().Be($"Group with key {groupKey} not found.");
            _groupRepositoryMock.Verify(r => r.Delete(It.IsAny<Group>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenGroupKeyIsEmpty_ShouldReturnBadRequest()
        {
            // Arrange
            var command = new DeleteGroupCommand(Guid.Empty);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.BadRequest);
            result.Data.Should().Be("GroupKey cannot be empty.");
            _groupRepositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<Guid>(), default), Times.Never);
            _groupRepositoryMock.Verify(r => r.Delete(It.IsAny<Group>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnServerError()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var group = new Group("Alpha", Guid.NewGuid()) { GroupKey = groupKey };
            var command = new DeleteGroupCommand(groupKey);

            _groupRepositoryMock.Setup(r => r.GetByKeyAsync(groupKey, default))
                .ReturnsAsync(group);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.InternalServerError);
            result.Data.Should().Be("Failed to delete Group due to internal error.");
            _groupRepositoryMock.Verify(r => r.Delete(group, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenDeleteThrowsException_ShouldPropagateException()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var group = new Group("Alpha", Guid.NewGuid()) { GroupKey = groupKey };
            var command = new DeleteGroupCommand(groupKey);
            var expected = new Exception("Test exception");

            _groupRepositoryMock.Setup(r => r.GetByKeyAsync(groupKey, default))
                .ReturnsAsync(group);
            _groupRepositoryMock.Setup(r => r.Delete(group, default))
                .Throws(expected);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, default));
            ex.Should().BeSameAs(expected);
        }
    }
}