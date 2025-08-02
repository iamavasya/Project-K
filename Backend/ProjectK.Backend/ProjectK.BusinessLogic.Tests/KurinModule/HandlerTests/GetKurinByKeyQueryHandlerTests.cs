using AutoMapper;
using Moq;
using ProjectK.API.MappingProfiles.KurinModule;
using ProjectK.BusinessLogic.Modules.Kurin.Queries;
using ProjectK.BusinessLogic.Modules.Kurin.Queries.Handlers;
using ProjectK.Common.Entities.Kurin;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests
{
    public class GetKurinByKeyQueryHandlerTests
    {
        private readonly IMapper _mapper;
        private readonly Mock<IKurinRepository> _kurinRepositoryMock;

        private readonly GetKurinByKeyQueryHandler _handler;

        public GetKurinByKeyQueryHandlerTests()
        {
            // Setup AutoMapper
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _kurinRepositoryMock = new Mock<IKurinRepository>();

            _handler = new GetKurinByKeyQueryHandler(_kurinRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_ShouldReturnKurin_WhenKurinExists()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var number = 51;
            var query = new GetKurinByKeyQuery { KurinKey = kurinKey };

            var kurinEntity = new Kurin
            {
                KurinKey = kurinKey,
                Number = number,
            };

            _kurinRepositoryMock
                .Setup(repo => repo.GetByKeyAsync(kurinKey))
                .ReturnsAsync(kurinEntity);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.KurinKey.Should().Be(kurinKey);
            result.Number.Should().Be(number);
        }

        [Fact]
        public async Task Handle_ShouldReturnNull_WhenKurinNotExists()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();;
            var query = new GetKurinByKeyQuery { KurinKey = kurinKey };
            Kurin? kurinEntity = null;

            _kurinRepositoryMock
                .Setup(repo => repo.GetByKeyAsync(kurinKey))
                .ReturnsAsync(kurinEntity);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.KurinKey.Should().BeEmpty();
            result.Number.Should().Be(0);
        }
    }
}
