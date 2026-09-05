using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule;
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
    public class AgendaAccessAuthorizeTargetTests
    {
        private readonly Mock<ICurrentUserContext> _currentUser = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IResourceScopeReader> _scopeReader = new();
        private readonly Mock<IResourceAccessService> _resourceAccess = new();
        private readonly Mock<ILeadershipRepository> _leaderships = new();
        private readonly AgendaAccess _access;

        private readonly Guid _kurinKey = Guid.NewGuid();
        private readonly Guid _groupKey = Guid.NewGuid();

        public AgendaAccessAuthorizeTargetTests()
        {
            _uow.Setup(u => u.Leaderships).Returns(_leaderships.Object);
            _access = new AgendaAccess(_currentUser.Object, _uow.Object, _scopeReader.Object, _resourceAccess.Object);
        }

        private AgendaTargetInput LeadershipTarget(Guid key) => new() { TargetType = AgendaTargetType.Leadership, TargetKey = key };

        [Fact]
        public async Task Authorize_GroupOffice_ChecksAccessOnItsGroup()
        {
            var leadership = new Leadership { Type = LeadershipType.Group, GroupKey = _groupKey, KurinKey = _kurinKey };
            _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(leadership);
            _resourceAccess.Setup(a => a.CheckAccessAsync(ResourceType.Group, ResourceAction.Create, _groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Allow("ok"));

            var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

            result.IsAllowed.Should().BeTrue();
            _resourceAccess.Verify(a => a.CheckAccessAsync(ResourceType.Group, ResourceAction.Create, _groupKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Authorize_KurinOrKvOffice_ChecksAccessOnTheKurin()
        {
            var leadership = new Leadership { Type = LeadershipType.KV, GroupKey = null, KurinKey = _kurinKey };
            _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(leadership);
            _resourceAccess.Setup(a => a.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Create, _kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ResourceAccessDecision.Allow("ok"));

            var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

            result.IsAllowed.Should().BeTrue();
            _resourceAccess.Verify(a => a.CheckAccessAsync(ResourceType.Kurin, ResourceAction.Create, _kurinKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Authorize_MissingLeadership_Denies()
        {
            _leaderships.Setup(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Leadership?)null);

            var result = await _access.AuthorizeTargetAsync(LeadershipTarget(Guid.NewGuid()), ResourceAction.Create);

            result.IsAllowed.Should().BeFalse();
            _resourceAccess.Verify(a => a.CheckAccessAsync(It.IsAny<ResourceType>(), It.IsAny<ResourceAction>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
