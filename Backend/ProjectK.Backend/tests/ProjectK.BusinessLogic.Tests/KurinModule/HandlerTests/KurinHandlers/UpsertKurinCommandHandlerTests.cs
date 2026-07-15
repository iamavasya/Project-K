using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Services.Caching;
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
        private readonly Mock<IBackendCache> _cacheMock;
        private readonly UpsertKurinHandler _handler;

        public UpsertKurinHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinModuleProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _kurinRepositoryMock = new Mock<IKurinRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _cacheMock = new Mock<IBackendCache>();

            _unitOfWorkMock.Setup(uow => uow.Kurins).Returns(_kurinRepositoryMock.Object);

            _handler = new UpsertKurinHandler(_unitOfWorkMock.Object, _mapper, _cacheMock.Object);
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
            _cacheMock.Verify(c => c.Invalidate(BackendCachePolicies.KurinReads), Times.Once);
            _cacheMock.Verify(c => c.Invalidate(BackendCachePolicies.GroupReads), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenUpdatingExistingKurin_ShouldUpdateAndReturnSuccess()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var oldNumber = 123;
            var newNumber = 456;
            var existingKurin = new Kurin(oldNumber) { KurinKey = kurinKey };
            var command = new UpsertKurin(kurinKey, newNumber, "  Kyiv  ", "  Ukraine  ", "  Some Patron  ", "  Long form notes  ", profileVerificationEnabled: true);

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
            existingKurin.Stanytsia.Should().Be("Kyiv");
            existingKurin.RegionOrCountry.Should().Be("Ukraine");
            existingKurin.NamedAfter.Should().Be("Some Patron");
            existingKurin.Description.Should().Be("Long form notes");
            existingKurin.ProfileVerificationEnabled.Should().BeTrue();
            result.Data.ProfileVerificationEnabled.Should().BeTrue();

            _kurinRepositoryMock.Verify(r => r.Update(existingKurin, default), Times.Once);
            _kurinRepositoryMock.Verify(r => r.Create(It.IsAny<Kurin>(), default), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Theory]
        [InlineData(121, null, null, "StanytsiaTooLong")]
        [InlineData(null, 121, null, "RegionOrCountryTooLong")]
        [InlineData(null, null, 201, "NamedAfterTooLong")]
        [InlineData(null, null, 4001, "DescriptionTooLong")]
        public async Task Handle_WhenProfileFieldsAreTooLong_ShouldReturnBadRequest(
            int? stanytsiaLength,
            int? regionLength,
            int? namedAfterOrDescriptionLength,
            string expectedErrorCode)
        {
            // Arrange
            var command = new UpsertKurin(
                Guid.NewGuid(),
                123,
                stanytsiaLength.HasValue ? new string('a', stanytsiaLength.Value) : null,
                regionLength.HasValue ? new string('a', regionLength.Value) : null,
                expectedErrorCode == "NamedAfterTooLong" ? new string('a', namedAfterOrDescriptionLength!.Value) : null,
                expectedErrorCode == "DescriptionTooLong" ? new string('a', namedAfterOrDescriptionLength!.Value) : null);

            // Act
            var result = await _handler.Handle(command, default);

            // Assert
            result.Type.Should().Be(ResultType.BadRequest);
            result.ErrorCode.Should().Be(expectedErrorCode);
            _kurinRepositoryMock.Verify(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
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
