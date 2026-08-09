using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Update;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class UpdateAgendaItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IAgendaItemRepository> _agendaRepo = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly UpdateAgendaItemHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();

        public UpdateAgendaItemHandlerTests()
        {
            _uow.Setup(u => u.AgendaItems).Returns(_agendaRepo.Object);
            _uow.Setup(u => u.Members).Returns(_memberRepo.Object);
            _memberRepo.Setup(r => r.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Member>());
            _currentUser.Setup(c => c.KurinKey).Returns(_kurinKey);
            _access.Setup(a => a.BuildViewerAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendaViewerContext(_kurinKey, Guid.NewGuid(), null, null, Array.Empty<Guid>(), true, true));
            _access.Setup(a => a.AuthorizeTargetAsync(It.IsAny<AgendaTargetInput>(), ResourceAction.Create, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Allow());
            _handler = new UpdateAgendaItemHandler(_uow.Object, _access.Object, _currentUser.Object, _notifications.Object);
        }

        // Regression: stretching an event by adding an end date while keeping the same target must not
        // churn the assignment rows (delete + re-insert would trip the unique index / concurrency check).
        [Fact]
        public async Task Handle_WhenAddingEndDateWithSameTarget_UpdatesInPlaceWithoutChurn()
        {
            var kurinTarget = new AgendaAssignment { TargetType = AgendaTargetType.Kurin, TargetKey = _kurinKey };
            var item = new AgendaItem
            {
                AgendaItemKey = Guid.NewGuid(),
                KurinKey = _kurinKey,
                Kind = AgendaItemKind.Event,
                Title = "Табір",
                StartUtc = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc),
                EndUtc = null,
                Assignments = new List<AgendaAssignment> { kurinTarget }
            };
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);

            var request = new UpdateAgendaItem
            {
                AgendaItemKey = item.AgendaItemKey,
                Kind = AgendaItemKind.Event,
                Title = "Табір",
                StartUtc = item.StartUtc,
                EndUtc = new DateTime(2026, 8, 14, 0, 0, 0, DateTimeKind.Utc),
                Targets = new List<AgendaTargetInput> { new() { TargetType = AgendaTargetType.Kurin, TargetKey = _kurinKey } }
            };

            var result = await _handler.Handle(request, default);

            result.Type.Should().Be(ResultType.Success);
            item.EndUtc.Should().Be(request.EndUtc);
            // Same target kept: no assignment churn, and never a whole-entity Update().
            _agendaRepo.Verify(r => r.RemoveAssignment(It.IsAny<AgendaAssignment>()), Times.Never);
            _agendaRepo.Verify(r => r.AddAssignment(It.IsAny<AgendaAssignment>()), Times.Never);
            _agendaRepo.Verify(r => r.Update(It.IsAny<AgendaItem>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenTargetsChange_RemovesGoneAddsNew()
        {
            var group = Guid.NewGuid();
            var keptKurin = new AgendaAssignment { TargetType = AgendaTargetType.Kurin, TargetKey = _kurinKey };
            var removedGroup = new AgendaAssignment { TargetType = AgendaTargetType.Group, TargetKey = group };
            var item = new AgendaItem
            {
                AgendaItemKey = Guid.NewGuid(),
                KurinKey = _kurinKey,
                Kind = AgendaItemKind.Task,
                Title = "Задача",
                Assignments = new List<AgendaAssignment> { keptKurin, removedGroup }
            };
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);

            var newMember = Guid.NewGuid();
            var request = new UpdateAgendaItem
            {
                AgendaItemKey = item.AgendaItemKey,
                Kind = AgendaItemKind.Task,
                Title = "Задача",
                Targets = new List<AgendaTargetInput>
                {
                    new() { TargetType = AgendaTargetType.Kurin, TargetKey = _kurinKey },
                    new() { TargetType = AgendaTargetType.Member, TargetKey = newMember }
                }
            };

            var result = await _handler.Handle(request, default);

            result.Type.Should().Be(ResultType.Success);
            // Gone target deleted, new target inserted, kept target untouched.
            _agendaRepo.Verify(r => r.RemoveAssignment(removedGroup), Times.Once);
            _agendaRepo.Verify(r => r.RemoveAssignment(keptKurin), Times.Never);
            _agendaRepo.Verify(r => r.AddAssignment(It.Is<AgendaAssignment>(a => a.TargetType == AgendaTargetType.Member && a.TargetKey == newMember)), Times.Once);
            _agendaRepo.Verify(r => r.AddAssignment(It.Is<AgendaAssignment>(a => a.TargetType == AgendaTargetType.Kurin)), Times.Never);
        }
    }
}
