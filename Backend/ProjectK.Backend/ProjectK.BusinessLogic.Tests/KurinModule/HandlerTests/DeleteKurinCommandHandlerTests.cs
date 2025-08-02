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

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests
{
    public class DeleteKurinCommandHandlerTests
    {
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly DeleteKurinCommandHandler _handler;
        public DeleteKurinCommandHandlerTests()
        {
            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _handler = new DeleteKurinCommandHandler(_kurinRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteKurin_WhenKurinExists()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new DeleteKurinCommand(kurinKey);
            _kurinRepositoryMock
                .Setup(repo => repo.DeleteAsync(kurinKey, CancellationToken.None))
                .ReturnsAsync(true);
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            result.Should().BeTrue();
            _kurinRepositoryMock.Verify(repo => repo.DeleteAsync(kurinKey, CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenKurinKeyIsEmpty()
        {
            // Arrange
            var command = new DeleteKurinCommand(Guid.Empty);
            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("KurinKey cannot be empty.*")
                .WithParameterName("KurinKey");
        }

        [Fact]
        public async Task Handle_ShouldReturnFalse_WhenKurinDoesNotExist()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var command = new DeleteKurinCommand(kurinKey);
            _kurinRepositoryMock
                .Setup(repo => repo.DeleteAsync(kurinKey, CancellationToken.None))
                .ReturnsAsync(false);
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            result.Should().BeFalse();
            _kurinRepositoryMock.Verify(repo => repo.DeleteAsync(kurinKey, CancellationToken.None), Times.Once);
        }
    }
}
