using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Status;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class ChangeAgendaItemStatusHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IAgendaItemRepository> _agendaRepo = new();
        private readonly ChangeAgendaItemStatusHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private readonly Guid _memberKey = Guid.NewGuid();
        private readonly Guid _actorUserKey = Guid.NewGuid();

        public ChangeAgendaItemStatusHandlerTests()
        {
            _uow.Setup(u => u.AgendaItems).Returns(_agendaRepo.Object);
            _currentUser.Setup(c => c.KurinKey).Returns(_kurinKey);
            _handler = new ChangeAgendaItemStatusHandler(_uow.Object, _access.Object, _currentUser.Object, _notifications.Object);
        }

        private AgendaItem TaskAssignedToMember(Guid? createdBy = null)
        {
            return new AgendaItem
            {
                AgendaItemKey = Guid.NewGuid(),
                KurinKey = _kurinKey,
                Kind = AgendaItemKind.Task,
                Status = AgendaItemStatus.Todo,
                // Creator == actor keeps the notification path quiet on the success cases.
                CreatedByUserKey = createdBy ?? _actorUserKey,
                Assignments = new List<AgendaAssignment>
                {
                    new() { TargetType = AgendaTargetType.Member, TargetKey = _memberKey }
                }
            };
        }

        private void SetupViewer(bool canSeeWholeKurin, bool isLeadership, Guid? viewerMemberKey)
        {
            _access.Setup(a => a.BuildViewerAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendaViewerContext(
                    KurinKey: _kurinKey,
                    ViewerUserKey: _actorUserKey,
                    ViewerMemberKey: viewerMemberKey,
                    ViewerOwnGroupKey: null,
                    VisibilityGroupKeys: Array.Empty<Guid>(),
                    CanSeeWholeKurin: canSeeWholeKurin,
                    IsLeadership: isLeadership));
        }

        [Fact]
        public async Task Handle_WhenViewerIsAssignee_ChangesStatus()
        {
            var item = TaskAssignedToMember();
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupViewer(canSeeWholeKurin: false, isLeadership: false, viewerMemberKey: _memberKey);

            var result = await _handler.Handle(new ChangeAgendaItemStatus(item.AgendaItemKey, AgendaItemStatus.Done), default);

            result.Type.Should().Be(ResultType.Success);
            item.Status.Should().Be(AgendaItemStatus.Done);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenViewerIsUnrelatedMember_ReturnsForbidden()
        {
            // A different creator so the "creator may manage" branch does not apply.
            var item = TaskAssignedToMember(createdBy: Guid.NewGuid());
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupViewer(canSeeWholeKurin: false, isLeadership: false, viewerMemberKey: Guid.NewGuid());

            var result = await _handler.Handle(new ChangeAgendaItemStatus(item.AgendaItemKey, AgendaItemStatus.Done), default);

            result.Type.Should().Be(ResultType.Forbidden);
            item.Status.Should().Be(AgendaItemStatus.Todo);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenViewerLeadsKurin_ChangesStatus()
        {
            var item = TaskAssignedToMember();
            _agendaRepo.Setup(r => r.GetByKeyWithAssignmentsAsync(item.AgendaItemKey, It.IsAny<CancellationToken>())).ReturnsAsync(item);
            SetupViewer(canSeeWholeKurin: true, isLeadership: true, viewerMemberKey: null);

            var result = await _handler.Handle(new ChangeAgendaItemStatus(item.AgendaItemKey, AgendaItemStatus.InProgress), default);

            result.Type.Should().Be(ResultType.Success);
            item.Status.Should().Be(AgendaItemStatus.InProgress);
        }
    }
}
