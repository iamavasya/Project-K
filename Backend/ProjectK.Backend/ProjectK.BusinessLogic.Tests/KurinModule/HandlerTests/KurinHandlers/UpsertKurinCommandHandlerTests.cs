using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.KurinHandlers
{
    public class UpsertKurinHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly UpsertKurinHandler _handler;

        public UpsertKurinHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinModuleProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(uow => uow.Kurins).Returns(_kurinRepositoryMock.Object);

            _handler = new UpsertKurinHandler(_unitOfWorkMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_WhenCreatingNewKurin_ShouldCreateAndReturnSuccess()
        {
            // Arrange
            var number = 123;
            var command = new UpsertKurin(number);
            Kurin savedKurin = null!;

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
            result.Type.Should().Be(ResultType.Created);
            result.Data.Should().NotBeNull();
            result.Data.Number.Should().Be(number);
            result.Data.KurinKey.Should().Be(savedKurin.KurinKey);

            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Once);
            _kurinRepositoryMock.Verify(r => r.Update(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUpdatingExistingKurin_ShouldUpdateAndReturnSuccess()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var oldNumber = 123;
            var newNumber = 456;
            var existingKurin = new Kurin(oldNumber) { KurinKey = kurinKey };
            var command = new UpsertKurin(kurinKey, newNumber);

            _kurinRepositoryMock.Setup(r => r.GetByKeyAsync(kurinKey, default))
                .ReturnsAsync(existingKurin);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data.Number.Should().Be(newNumber);
            result.Data.KurinKey.Should().Be(kurinKey);

            existingKurin.Number.Should().Be(newNumber);

            _kurinRepositoryMock.Verify(r => r.Update(existingKurin, default), Times.Once);
            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenSaveChangesFails_ShouldReturnInternalServerError()
        {
            // Arrange
            var number = 123;
            var command = new UpsertKurin(number);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(0);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.InternalServerError);
            result.Data.Should().BeNull();

            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Once);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var number = 123;
            var command = new UpsertKurin(kurinKey, number);
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
            var command = new UpsertKurin(number);
            Kurin savedKurin = null!;

            _kurinRepositoryMock.Setup(r => r.Create(It.IsAny<Kurin>(), default))
                .Callback<Kurin, CancellationToken>((k, _) => savedKurin = k);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(default)).ReturnsAsync(1);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            var directlyMapped = _mapper.Map<KurinResponse>(savedKurin);
            result.Data.Should().BeEquivalentTo(directlyMapped);
        }
    }
}
