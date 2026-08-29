using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Agenda.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class GetAssignTargetsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IAgendaAccess> _access = new();
        private readonly Mock<IGroupRepository> _groupRepo = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly Mock<ILeadershipRepository> _leadershipRepo = new();
        private readonly GetAssignTargetsHandler _handler;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private readonly Guid _g1 = Guid.NewGuid();
        private readonly Guid _g2 = Guid.NewGuid();

        public GetAssignTargetsHandlerTests()
        {
            var groups = new List<Group>
            {
                new("Gurtok 1", _kurinKey) { GroupKey = _g1 },
                new("Gurtok 2", _kurinKey) { GroupKey = _g2 }
            };
            var members = new List<Member>
            {
                new() { MemberKey = Guid.NewGuid(), KurinKey = _kurinKey, GroupKey = _g1, FirstName = "A", LastName = "One" },
                new() { MemberKey = Guid.NewGuid(), KurinKey = _kurinKey, GroupKey = _g2, FirstName = "B", LastName = "Two" }
            };

            _uow.Setup(u => u.Groups).Returns(_groupRepo.Object);
            _uow.Setup(u => u.Members).Returns(_memberRepo.Object);
            _uow.Setup(u => u.Leaderships).Returns(_leadershipRepo.Object);
            _groupRepo.Setup(r => r.GetAllAsync(_kurinKey, It.IsAny<CancellationToken>())).ReturnsAsync(groups);
            _memberRepo.Setup(r => r.GetAllByKurinKeyAsync(_kurinKey, It.IsAny<CancellationToken>())).ReturnsAsync(members);
            _leadershipRepo.Setup(r => r.GetLeadershipRefsForKurinAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<LeadershipRef>());

            _handler = new GetAssignTargetsHandler(_uow.Object, _access.Object);
        }

        private void SetupViewer(bool canSeeWholeKurin, IReadOnlyCollection<Guid> visibilityGroups)
        {
            _access.Setup(a => a.BuildViewerAsync(_kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AgendaViewerContext(
                    _kurinKey, Guid.NewGuid(), null, null, visibilityGroups, Array.Empty<Guid>(), canSeeWholeKurin, canSeeWholeKurin));
        }

        [Fact]
        public async Task Handle_ForManager_ReturnsWholeKurinAndAllGroups()
        {
            SetupViewer(canSeeWholeKurin: true, visibilityGroups: Array.Empty<Guid>());
            _access.Setup(a => a.AuthorizeTargetAsync(It.IsAny<AgendaTargetInput>(), ResourceAction.Create, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Allow());

            var result = await _handler.Handle(new GetAssignTargets(_kurinKey), default);

            result.Type.Should().Be(ResultType.Success);
            result.Data!.CanTargetKurin.Should().BeTrue();
            result.Data.Groups.Should().HaveCount(2);
        }

        [Fact]
        public async Task Handle_ForMentor_LimitsToAssignedGroupsAndHidesKurin()
        {
            SetupViewer(canSeeWholeKurin: false, visibilityGroups: new[] { _g1 });
            _access.Setup(a => a.AuthorizeTargetAsync(It.IsAny<AgendaTargetInput>(), ResourceAction.Create, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Deny("Mentor cannot target the whole kurin."));

            var result = await _handler.Handle(new GetAssignTargets(_kurinKey), default);

            result.Type.Should().Be(ResultType.Success);
            result.Data!.CanTargetKurin.Should().BeFalse();
            result.Data.Groups.Should().ContainSingle().Which.GroupKey.Should().Be(_g1);
        }
    }
}
