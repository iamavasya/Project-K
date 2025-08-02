using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests
{
    public class CreateKurinCommandHandlerTests
    {
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;

        private readonly CreateKurinCommandHandler _handler;
        public CreateKurinCommandHandlerTests()
        {
            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _handler = new CreateKurinCommandHandler(_kurinRepositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateKurinAndReturnKey()
        {
            // Arrange
            var number = 51;
            var command = new CreateKurinCommand { Number = number };
            var expectedKurinKey = Guid.NewGuid();
            _kurinRepositoryMock
                .Setup(repo => repo.CreateAsync(It.IsAny<Common.Entities.KurinModule.Kurin>(), CancellationToken.None))
                .ReturnsAsync(expectedKurinKey);
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            result.Should().Be(expectedKurinKey);
            _kurinRepositoryMock.Verify(repo => repo.CreateAsync(It.IsAny<Common.Entities.KurinModule.Kurin>(), CancellationToken.None), Times.Once);
        }
    }
}
