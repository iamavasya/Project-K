using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Delete;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class DeleteAgendaItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IAgendaItemRepository> _agendaRepo = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly DeleteAgendaItemHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private readonly Guid _creatorKey = Guid.NewGuid();

        public DeleteAgendaItemHandlerTests()
        {
            _uow.Setup(u => u.AgendaItems).Returns(_agendaRepo.Object);
            _uow.Setup(u => u.Members).Returns(_memberRepo.Object);
            _memberRepo.Setup(r => r.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Member>());
            _currentUser.Setup(c => c.KurinKey).Returns(_kurinKey);
            _handler = new DeleteAgendaItemHandler(_uow.Object, _access.Object, _currentUser.Object, _notifications.Object);
        }

        private AgendaItem Item() => new()
        {
            AgendaItemKey = Guid.NewGuid(),
            KurinKey = _kurinKey,
            Kind = AgendaItemKind.Event,
            Title = "Подія",
            CreatedByUserKey = _creatorKey,
            Assignments = new List<AgendaAssignment>()
        };

        private void SetupViewer(bool canSeeWholeKurin, bool isLeadership, Guid? viewerUserKey)
        {
            _access.Setup(a => a.BuildViewerAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendaViewerContext(
                    _kurinKey, viewerUserKey, null, null, Array.Empty<Guid>(), Array.Empty<Guid>(), canSeeWholeKurin, isLeadership));
        }

        [Fact]
        public async Task Handle_WhenItemMissing_ReturnsNotFound()
        {
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AgendaItem?)null);

            var result = await _handler.Handle(new DeleteAgendaItem(Guid.NewGuid()), default);

            result.Type.Should().Be(ResultType.NotFound);
        }

        [Fact]
        public async Task Handle_WhenPlainMemberNotCreator_ReturnsForbidden()
        {
            var item = Item();
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupViewer(canSeeWholeKurin: false, isLeadership: false, viewerUserKey: Guid.NewGuid());

            var result = await _handler.Handle(new DeleteAgendaItem(item.AgendaItemKey), default);

            result.Type.Should().Be(ResultType.Forbidden);
            _agendaRepo.Verify(r => r.Delete(It.IsAny<AgendaItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenCreator_DeletesAndSaves()
        {
            var item = Item();
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupViewer(canSeeWholeKurin: false, isLeadership: false, viewerUserKey: _creatorKey);

            var result = await _handler.Handle(new DeleteAgendaItem(item.AgendaItemKey), default);

            result.Type.Should().Be(ResultType.Success);
            _agendaRepo.Verify(r => r.Delete(item, It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
