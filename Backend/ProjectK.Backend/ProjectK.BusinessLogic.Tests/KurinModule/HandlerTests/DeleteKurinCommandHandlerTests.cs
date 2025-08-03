using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using FluentAssertions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests
{
    public class DeleteKurinCommandHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly DeleteKurinCommandHandler _handler;
        public DeleteKurinCommandHandlerTests()
        {
            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(uow => uow.Kurins).Returns(_kurinRepositoryMock.Object);

            _handler = new DeleteKurinCommandHandler(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task Handle_WhenKurinExists_ShouldDeleteKurinAndReturnTrue()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var kurin = new Kurin(1) { KurinKey = kurinKey };
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(kurin);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Should().BeTrue();
            _kurinRepositoryMock.Verify(r => r.Delete(kurin, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenKurinDoesNotExist_ShouldReturnFalse()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync((Kurin)null);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Should().BeFalse();
            _kurinRepositoryMock.Verify(r => r.Delete(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenExceptionOccurs_ShouldPropagateException()
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

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, default));
            exception.Should().BeSameAs(expectedException);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnFalse()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var kurin = new Kurin(1) { KurinKey = kurinKey };
            var command = new DeleteKurinCommand(kurinKey);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(kurin);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(0); // No changes saved

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Should().BeFalse();
            _kurinRepositoryMock.Verify(r => r.Delete(kurin, default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }
    }
}
