using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Responses;
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
    public class GetAgendaResponsesHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<IAgendaItemRepository> _items = new();
        private readonly Mock<IAgendaResponseRepository> _responses = new();
        private readonly Mock<IMemberRepository> _members = new();
        private readonly Mock<IAgendaCategoryRepository> _categories = new();
        private readonly GetAgendaResponsesHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private readonly Guid _userKey = Guid.NewGuid();

        public GetAgendaResponsesHandlerTests()
        {
            _uow.Setup(u => u.AgendaItems).Returns(_items.Object);
            _uow.Setup(u => u.AgendaResponses).Returns(_responses.Object);
            _uow.Setup(u => u.Members).Returns(_members.Object);
            _uow.Setup(u => u.AgendaCategories).Returns(_categories.Object);
            _currentUser.Setup(c => c.UserId).Returns(_userKey);
            _currentUser.Setup(c => c.KurinKey).Returns(_kurinKey);
            _members.Setup(m => m.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<Member>());
            _responses.Setup(r => r.GetForItemAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AgendaResponse>());
            _handler = new GetAgendaResponsesHandler(_uow.Object, _access.Object, _currentUser.Object);
        }

        private AgendaItem Event(Guid? kurin = null) => new()
        {
            AgendaItemKey = Guid.NewGuid(),
            KurinKey = kurin ?? _kurinKey,
            Kind = AgendaItemKind.Event,
            Assignments = new List<AgendaAssignment> { new() { TargetType = AgendaTargetType.Kurin, TargetKey = kurin ?? _kurinKey } }
        };

        private void SetupVisible(Guid kurinKey, bool visible) =>
            _access.Setup(a => a.BuildViewerAsync(kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendaViewerContext(kurinKey, _userKey, null, null,
                    Array.Empty<Guid>(), Array.Empty<Guid>(), CanSeeWholeKurin: visible, IsLeadership: visible));

        [Fact]
        public async Task Handle_ForEventInAnotherKurin_ReturnsForbidden()
        {
            var item = Event(kurin: Guid.NewGuid());
            _items.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);

            var result = await _handler.Handle(new GetAgendaResponses(item.AgendaItemKey), default);

            result.Type.Should().Be(ResultType.Forbidden);
        }

        [Fact]
        public async Task Handle_WhenNotVisible_ReturnsForbidden()
        {
            var item = Event();
            item.Assignments.Clear();
            _items.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupVisible(_kurinKey, visible: false);

            var result = await _handler.Handle(new GetAgendaResponses(item.AgendaItemKey), default);

            result.Type.Should().Be(ResultType.Forbidden);
        }

        [Fact]
        public async Task Handle_WhenVisible_ReturnsProjectedPicture()
        {
            var item = Event();
            _items.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupVisible(_kurinKey, visible: true);
            _responses.Setup(r => r.GetForItemAsync(item.AgendaItemKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AgendaResponse>
                {
                    new() { AgendaItemKey = item.AgendaItemKey, UserKey = _userKey, Status = AgendaRsvpStatus.Going, RespondedAtUtc = DateTime.UtcNow },
                    new() { AgendaItemKey = item.AgendaItemKey, UserKey = Guid.NewGuid(), Status = AgendaRsvpStatus.Maybe, RespondedAtUtc = DateTime.UtcNow }
                });

            var result = await _handler.Handle(new GetAgendaResponses(item.AgendaItemKey), default);

            result.Type.Should().Be(ResultType.Success);
            result.Data!.GoingConfirmedCount.Should().Be(1);
            result.Data.MaybeCount.Should().Be(1);
            result.Data.MyStatus.Should().Be(AgendaRsvpStatus.Going);
        }
    }
}
