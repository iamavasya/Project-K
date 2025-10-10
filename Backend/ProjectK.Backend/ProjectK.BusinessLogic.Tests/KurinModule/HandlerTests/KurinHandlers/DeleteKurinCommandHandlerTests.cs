using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins.Handlers;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.KurinHandlers
{
    public class DeleteKurinCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly DeleteKurinCommandHandler _handler;
        private readonly Mock<IMemberRepository> _memberRepositoryMock;

        public DeleteKurinCommandHandlerTests()
        {
            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _memberRepositoryMock = new Mock<IMemberRepository>();

            _unitOfWorkMock.Setup(uow => uow.Kurins).Returns(_kurinRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.Members).Returns(_memberRepositoryMock.Object);

            _handler = new DeleteKurinCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenKurinExists_ShouldDeleteKurinAndReturnSuccess()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var kurin = new Kurin(1) { KurinKey = kurinKey };
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(kurin);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(1);
            _memberRepositoryMock.Setup(r => r.GetAllByKurinKeyAsync(kurinKey, default))
                .ReturnsAsync([]);
            _memberRepositoryMock.Setup(r => r.Delete(It.IsAny<Member>(), default));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().BeNull();
            _kurinRepositoryMock.Verify(r => r.Delete(kurin, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenKurinDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync((Kurin)null!);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().Be($"Kurin with key {kurinKey} not found.");
            _kurinRepositoryMock.Verify(r => r.Delete(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenKurinKeyIsEmpty_ShouldReturnInvalidData()
        {
            // Arrange
            var command = new DeleteKurinCommand(Guid.Empty);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.BadRequest);
            result.Data.Should().Be("KurinKey cannot be empty.");
            _kurinRepositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<Guid>(), default), Times.Never);
            _kurinRepositoryMock.Verify(r => r.Delete(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnServerError()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var kurin = new Kurin(1) { KurinKey = kurinKey };
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(kurin);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(0);
            _memberRepositoryMock.Setup(r => r.GetAllByKurinKeyAsync(kurinKey, default))
                .ReturnsAsync([]);
            _memberRepositoryMock.Setup(r => r.Delete(It.IsAny<Member>(), default));

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.InternalServerError);
            result.Data.Should().Be("Failed to delete Kurin due to internal error.");
            _kurinRepositoryMock.Verify(r => r.Delete(kurin, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenDeleteThrowsException_ShouldPropagateException()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var kurin = new Kurin(1) { KurinKey = kurinKey };
            var command = new DeleteKurinCommand(kurinKey);
            var expectedException = new Exception("Test exception");

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(kurin);
            _kurinRepositoryMock.Setup(r => r.Delete(kurin, default))
                .Throws(expectedException);
            _memberRepositoryMock.Setup(r => r.GetAllByKurinKeyAsync(kurinKey, default))
                .ReturnsAsync([]);
            _memberRepositoryMock.Setup(r => r.Delete(It.IsAny<Member>(), default));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, default));
            exception.Should().BeSameAs(expectedException);
        }
    }
}
