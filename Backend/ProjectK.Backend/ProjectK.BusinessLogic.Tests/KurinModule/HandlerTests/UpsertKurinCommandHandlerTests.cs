using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.MappingProfiles.KurinModule;
using ProjectK.BusinessLogic.Modules.Kurin.Queries.Handlers;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Handler;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;
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
        private readonly UpsertKurinCommandHandler _handler;
        public UpsertKurinCommandHandlerTests()
        {
            // Setup AutoMapper
            var loggerFactory = LoggerFactory.Create(builder => { });
            var config = new MapperConfiguration(cfg => cfg.AddProfile(new KurinProfile()), loggerFactory);
            _mapper = config.CreateMapper();

            _kurinRepositoryMock = new Mock<IKurinRepository>();

            _handler = new UpsertKurinCommandHandler(_kurinRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task Handle_ShouldReturnKurinResponse_WhenKurinIsUpserted()
        {
            // Arrange
            var kurinKey = Guid.NewGuid();
            var number = 51;
            var command = new UpsertKurinCommand(kurinKey, number);
            var kurinEntity = new Kurin
            {
                KurinKey = kurinKey,
                Number = number,
            };
            _kurinRepositoryMock
                .Setup(repo => repo.GetByKeyOrCreateAsync(It.IsAny<KurinDto>(), CancellationToken.None))
                .ReturnsAsync(kurinEntity);
            // Act
            var result = await _handler.Handle(command, CancellationToken.None);
            // Assert
            result.Should().NotBeNull();
            result.KurinKey.Should().Be(kurinKey);
            result.Number.Should().Be(number);
        }
    }
}
