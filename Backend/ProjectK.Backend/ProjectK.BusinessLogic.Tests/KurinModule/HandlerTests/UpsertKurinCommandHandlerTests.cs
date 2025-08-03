using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles.KurinModule;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.BusinessLogic.Modules.Kurin.Queries.Handlers;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests
{
    public class UpsertKurinCommandHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly UpsertKurinCommandHandler _handler;
        public UpsertKurinCommandHandlerTests()
        {
            // Setup AutoMapper
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(uow => uow.Kurins).Returns(_kurinRepositoryMock.Object);

            _handler = new UpsertKurinCommandHandler(_unitOfWorkMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_WhenCreatingNewKurin_ShouldCreateAndReturnResponse()
        {
            // Arrange
            var number = 123;
            var command = new UpsertKurinCommand(number);
            var savedKurin = new Kurin(number);

            _kurinRepositoryMock.Setup(r => r.Create(It.IsAny<Kurin>(), default))
                .Callback<Kurin, CancellationToken>((k, _) =>
                {
                    k.KurinKey.Should().NotBeEmpty();
                    k.Number.Should().Be(number);
                    savedKurin = k;
                });

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Should().NotBeNull();
            result.Number.Should().Be(number);
            result.KurinKey.Should().Be(savedKurin.KurinKey);

            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Once);
            _kurinRepositoryMock.Verify(r => r.Update(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUpdatingExistingKurin_ShouldUpdateAndReturnResponse()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var oldNumber = 123;
            var newNumber = 456;
            var existingKurin = new Kurin(oldNumber) { KurinKey = kurinKey };
            var command = new UpsertKurinCommand(kurinKey, newNumber);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(existingKurin);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Should().NotBeNull();
            result.Number.Should().Be(newNumber);
            result.KurinKey.Should().Be(kurinKey);

            existingKurin.Number.Should().Be(newNumber);

            _kurinRepositoryMock.Verify(r => r.Update(existingKurin, default), Times.Once);
            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldThrowException()
        {
            // Arrange
            var number = 123;
            var command = new UpsertKurinCommand(number);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(0);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, default));

            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var number = 123;
            var command = new UpsertKurinCommand(kurinKey, number);
            var expectedException = new Exception("Database error");

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ThrowsAsync(expectedException);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, default));
            exception.Should().BeSameAs(expectedException);
        }

        [Fact]
        public async Task Handle_WhenMappingWorks_ShouldMapCorrectly()
        {
            // Arrange
            var number = 123;
            var command = new UpsertKurinCommand(number);
            var savedKurin = new Kurin(number);

            _kurinRepositoryMock.Setup(r => r.Create(It.IsAny<Kurin>(), default))
                .Callback<Kurin, CancellationToken>((k, _) => savedKurin = k);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            var directlyMapped = _mapper.Map<KurinResponse>(savedKurin);
            result.Should().BeEquivalentTo(directlyMapped);
        }
    }
}
