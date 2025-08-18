using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Groups.Handlers;
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
    public class GetGroupByKeyQueryHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IGroupRepository> _groupRepositoryMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly GetGroupByKeyQueryHandler _handler;

        public GetGroupByKeyQueryHandlerTests()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinModuleProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _groupRepositoryMock = new Mock<IGroupRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();

            _unitOfWorkMock.Setup(u => u.Groups).Returns(_groupRepositoryMock.Object);

            _handler = new GetGroupByKeyQueryHandler(_unitOfWorkMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_WhenGroupExists_ShouldReturnSuccessWithMappedResponse()
        {
            // Arrange
            var kurin = new Kurin(17) { KurinKey = Guid.NewGuid() };
            var groupKey = Guid.NewGuid();
            var group = new Group("Alpha", kurin.KurinKey)
            {
                GroupKey = groupKey,
                Kurin = kurin
            };
            var query = new GetGroupByKeyQuery(groupKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data.GroupKey.Should().Be(groupKey);
            result.Data.KurinKey.Should().Be(kurin.KurinKey);
            result.Data.Name.Should().Be("Alpha");
            result.Data.KurinNumber.Should().Be(kurin.Number);

            _groupRepositoryMock.Verify(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenGroupDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var query = new GetGroupByKeyQuery(groupKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Group)null!);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.NotFound);
            result.Data.Should().BeNull();

            _groupRepositoryMock.Verify(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrowsException_ShouldPropagateException()
        {
            // Arrange
            var groupKey = Guid.NewGuid();
            var query = new GetGroupByKeyQuery(groupKey);
            var expected = new Exception("DB failure");

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(query, CancellationToken.None));

            ex.Should().BeSameAs(expected);
            _groupRepositoryMock.Verify(r => r.GetByKeyAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenMappingWorks_ShouldMapCorrectly()
        {
            // Arrange
            var kurin = new Kurin(99) { KurinKey = Guid.NewGuid() };
            var group = new Group("Bravo", kurin.KurinKey)
            {
                GroupKey = Guid.NewGuid(),
                Kurin = kurin
            };
            var query = new GetGroupByKeyQuery(group.GroupKey);

            _groupRepositoryMock
                .Setup(r => r.GetByKeyAsync(group.GroupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(group);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Type.Should().Be(ResultType.Success);
            var directlyMapped = _mapper.Map<GroupResponse>(group);
            result.Data.Should().BeEquivalentTo(directlyMapped);
        }
    }
}