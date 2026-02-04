using AutoMapper;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.LeadershipHandlers
{
    public class UpsertLeadershipHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<ILeadershipRepository> _leadershipRepoMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly UpsertLeadershipHandler _handler;

        public UpsertLeadershipHandlerTests()
        {
            _unitOfWorkMock.Setup(u => u.Leaderships).Returns(_leadershipRepoMock.Object);
            _handler = new UpsertLeadershipHandler(_unitOfWorkMock.Object, _mapperMock.Object);
        }

        private static UpsertLeadershipRequest BuildRequest(string type = "kurin") => new()
        {
            Type = type,
            EntityKey = Guid.NewGuid(),
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = null,
            LeadershipHistories = new List<LeadershipHistoryMemberDto>()
        };

        private static Leadership BuildLeadershipEntity(Guid? key = null) => new()
        {
            LeadershipKey = key ?? Guid.NewGuid(),
            StartDate = new DateOnly(2024, 1, 1)
        };

        private void SetupCreateMapping(UpsertLeadership command, Leadership entity)
        {
            _mapperMock
                .Setup(m => m.Map<Leadership>(command))
                .Returns(() =>
                {
                    // emulate mapping from command to new entity
                    entity.LeadershipKey = command.LeadershipKey ?? entity.LeadershipKey;
                    entity.StartDate = command.StartDate;
                    entity.EndDate = command.EndDate;
                    return entity;
                });

            _mapperMock
                .Setup(m => m.Map<LeadershipDto>(It.IsAny<Leadership>()))
                .Returns((Leadership src) => new LeadershipDto
                {
                    LeadershipKey = src.LeadershipKey,
                    Type = src.Type,
                    KurinKey = src.KurinKey,
                    GroupKey = src.GroupKey,
                    StartDate = src.StartDate,
                    EndDate = src.EndDate,
                    LeadershipHistories = new List<LeadershipHistoryMemberDto>()
                });
        }

        private void SetupUpdateMapping(UpsertLeadership command, Leadership existing)
        {
            _mapperMock
                .Setup(m => m.Map(command, existing))
                .Returns(() =>
                {
                    existing.StartDate = command.StartDate;
                    existing.EndDate = command.EndDate;
                    return existing;
                });

            _mapperMock
                .Setup(m => m.Map<LeadershipDto>(It.IsAny<Leadership>()))
                .Returns((Leadership src) => new LeadershipDto
                {
                    LeadershipKey = src.LeadershipKey,
                    Type = src.Type,
                    KurinKey = src.KurinKey,
                    GroupKey = src.GroupKey,
                    StartDate = src.StartDate,
                    EndDate = src.EndDate,
                    LeadershipHistories = new List<LeadershipHistoryMemberDto>()
                });
        }

        [Fact]
        public async Task Handle_ShouldCreateLeadership_WhenNoExistingFound()
        {
            var requestDto = BuildRequest("kurin");
            var command = new UpsertLeadership(requestDto);
            var entity = BuildLeadershipEntity();

            _leadershipRepoMock
                .Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Leadership?)null);

            SetupCreateMapping(command, entity);

            _unitOfWorkMock
                .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            Assert.NotNull(result.Data);
            Assert.Equal(entity.LeadershipKey, result.Data!.LeadershipKey);
            Assert.Equal("GetLeadershipByKey", result.CreatedAtActionName);
            Assert.NotNull(result.CreatedAtRouteValues);

            _leadershipRepoMock.Verify(r => r.Add(entity, It.IsAny<CancellationToken>()), Times.Once);
            _leadershipRepoMock.Verify(r => r.Update(It.IsAny<Leadership>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldCreateLeadership_WhenProvidedKeyNotFound()
        {
            var providedKey = Guid.NewGuid();
            var requestDto = BuildRequest("group");
            var command = new UpsertLeadership(requestDto, providedKey);
            var entity = BuildLeadershipEntity(providedKey);

            _leadershipRepoMock
                .Setup(r => r.GetByKeyAsync(providedKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Leadership?)null);

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            Assert.Equal(providedKey, result.Data!.LeadershipKey);
            _leadershipRepoMock.Verify(r => r.Add(It.Is<Leadership>(l => l.LeadershipKey == providedKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldAssignKurinKey_OnKurinTypeCreation()
        {
            var requestDto = BuildRequest("kurin");
            var command = new UpsertLeadership(requestDto);
            var entity = BuildLeadershipEntity();

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            Assert.Equal(LeadershipType.Kurin, result.Data!.Type);
            Assert.Equal(requestDto.EntityKey, result.Data.KurinKey);
            Assert.Null(result.Data.GroupKey);
        }

        [Fact]
        public async Task Handle_ShouldAssignGroupKey_OnGroupTypeCreation()
        {
            var requestDto = BuildRequest("group");
            var command = new UpsertLeadership(requestDto);
            var entity = BuildLeadershipEntity();

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            Assert.Equal(LeadershipType.Group, result.Data!.Type);
            Assert.Equal(requestDto.EntityKey, result.Data.GroupKey);
            Assert.Null(result.Data.KurinKey);
        }

        [Fact]
        public async Task Handle_ShouldAssignKurinKey_OnKvTypeCreation()
        {
            var requestDto = BuildRequest("kv"); // KV behaves like Kurin for key assignment
            var command = new UpsertLeadership(requestDto);
            var entity = BuildLeadershipEntity();

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            Assert.Equal(LeadershipType.KV, result.Data!.Type);
            Assert.Equal(requestDto.EntityKey, result.Data.KurinKey);
            Assert.Null(result.Data.GroupKey);
        }

        [Fact]
        public async Task Handle_ShouldUpdateLeadership_WhenExistingFound()
        {
            var existing = BuildLeadershipEntity();
            existing.Type = LeadershipType.Group;
            existing.GroupKey = Guid.NewGuid();

            var requestDto = BuildRequest("kurin"); // change type
            var command = new UpsertLeadership(requestDto, existing.LeadershipKey);

            _leadershipRepoMock
                .Setup(r => r.GetByKeyAsync(existing.LeadershipKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            SetupUpdateMapping(command, existing);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Success, result.Type);
            Assert.Equal(existing.LeadershipKey, result.Data!.LeadershipKey);
            Assert.Equal(LeadershipType.Kurin, result.Data.Type);
            Assert.Equal(requestDto.EntityKey, result.Data.KurinKey);
            Assert.Null(result.Data.GroupKey);

            _leadershipRepoMock.Verify(r => r.Update(existing, It.IsAny<CancellationToken>()), Times.Once);
            _leadershipRepoMock.Verify(r => r.Add(It.IsAny<Leadership>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnInternalServerError_WhenNoChangesPersisted_OnCreate()
        {
            var requestDto = BuildRequest("kurin");
            var command = new UpsertLeadership(requestDto);
            var entity = BuildLeadershipEntity();

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.InternalServerError, result.Type);
            Assert.Null(result.Data);
            _mapperMock.Verify(m => m.Map<LeadershipDto>(It.IsAny<Leadership>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldReturnInternalServerError_WhenNoChangesPersisted_OnUpdate()
        {
            var existing = BuildLeadershipEntity();
            existing.Type = LeadershipType.Kurin;
            existing.KurinKey = Guid.NewGuid();

            var requestDto = BuildRequest("group");
            var command = new UpsertLeadership(requestDto, existing.LeadershipKey);

            _leadershipRepoMock
                .Setup(r => r.GetByKeyAsync(existing.LeadershipKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            SetupUpdateMapping(command, existing);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.InternalServerError, result.Type);
            Assert.Null(result.Data);
            _mapperMock.Verify(m => m.Map<LeadershipDto>(It.IsAny<Leadership>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldThrowArgumentException_WhenInvalidType()
        {
            var requestDto = BuildRequest("INVALID_TYPE");
            var command = new UpsertLeadership(requestDto);

            var entity = BuildLeadershipEntity();
            // Mapping for creation still needed before it attempts Enum.Parse
            _mapperMock
                .Setup(m => m.Map<Leadership>(command))
                .Returns(entity);

            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            await Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ShouldTreatGuidEmptyLeadershipKey_AsCreate()
        {
            var requestDto = BuildRequest("group");
            // Mimic constructing with an empty leadership key (edge case)
            var command = new UpsertLeadership(requestDto, Guid.Empty);
            var entity = BuildLeadershipEntity();

            SetupCreateMapping(command, entity);
            _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            var result = await _handler.Handle(command, CancellationToken.None);

            Assert.Equal(ResultType.Created, result.Type);
            _leadershipRepoMock.Verify(r => r.Add(It.IsAny<Leadership>(), It.IsAny<CancellationToken>()), Times.Once);
            _leadershipRepoMock.Verify(r => r.Update(It.IsAny<Leadership>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
