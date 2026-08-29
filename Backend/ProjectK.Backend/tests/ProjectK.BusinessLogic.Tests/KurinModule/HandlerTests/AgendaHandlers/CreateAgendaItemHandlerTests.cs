using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Create;
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
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class CreateAgendaItemHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<INotificationService> _notifications = new();
        private readonly Mock<IAgendaItemRepository> _agendaRepo = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly CreateAgendaItemHandler _handler;

        public CreateAgendaItemHandlerTests()
        {
            _uow.Setup(u => u.AgendaItems).Returns(_agendaRepo.Object);
            _uow.Setup(u => u.Members).Returns(_memberRepo.Object);
            _memberRepo.Setup(r => r.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Member>());
            _handler = new CreateAgendaItemHandler(_uow.Object, _access.Object, _currentUser.Object, _notifications.Object);
        }

        private static CreateAgendaItem CommandWith(Guid kurinKey, params AgendaTargetInput[] targets) => new()
        {
            KurinKey = kurinKey,
            Kind = AgendaItemKind.Task,
            Title = "Прибирання",
            Targets = targets.ToList()
        };

        [Fact]
        public async Task Handle_WhenActorMissing_ReturnsUnauthorized()
        {
            _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

            var result = await _handler.Handle(CommandWith(Guid.NewGuid()), default);

            result.Type.Should().Be(ResultType.Unauthorized);
            _agendaRepo.Verify(r => r.Create(It.IsAny<AgendaItem>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenAnyTargetDenied_ReturnsForbiddenAndDoesNotSave()
        {
            var kurinKey = Guid.NewGuid();
            var group = new AgendaTargetInput { TargetType = AgendaTargetType.Group, TargetKey = Guid.NewGuid() };
            _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _access.Setup(a => a.AuthorizeTargetAsync(group, ResourceAction.Create, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Deny("Mentor has access only to assigned groups."));

            var result = await _handler.Handle(CommandWith(kurinKey, group), default);

            result.Type.Should().Be(ResultType.Forbidden);
            result.ErrorCode.Should().Be("AGENDA_TARGET_FORBIDDEN");
            _agendaRepo.Verify(r => r.Create(It.IsAny<AgendaItem>(), It.IsAny<CancellationToken>()), Times.Never);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenAllTargetsAllowed_CreatesAndReturnsCreated()
        {
            var kurinKey = Guid.NewGuid();
            var target = new AgendaTargetInput { TargetType = AgendaTargetType.Group, TargetKey = Guid.NewGuid() };
            _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
            _access.Setup(a => a.AuthorizeTargetAsync(It.IsAny<AgendaTargetInput>(), ResourceAction.Create, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Allow());

            var result = await _handler.Handle(CommandWith(kurinKey, target), default);

            result.Type.Should().Be(ResultType.Created);
            result.Data.Should().NotBe(Guid.Empty);
            _agendaRepo.Verify(r => r.Create(It.Is<AgendaItem>(a => a.KurinKey == kurinKey && a.Assignments.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
            _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
