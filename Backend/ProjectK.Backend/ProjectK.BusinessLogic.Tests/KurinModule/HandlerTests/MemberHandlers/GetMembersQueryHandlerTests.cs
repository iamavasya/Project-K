using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectK.API.MappingProfiles;
using ProjectK.API.MappingProfiles.Resolvers;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members;
using ProjectK.BusinessLogic.Modules.KurinModule.Queries.Members.Handlers;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers
{
    public class GetMembersQueryHandlerTests
    {
        private readonly Mock<IUnitOfWork> _uowMock;
        private readonly Mock<IMemberRepository> _memberRepoMock;
        private readonly IMapper _mapper;
        private readonly GetMembersQueryHandler _handler;

        public GetMembersQueryHandlerTests()
        {
            _memberRepoMock = new Mock<IMemberRepository>();
            _uowMock = new Mock<IUnitOfWork>();
            _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);

            var loggerFactory = LoggerFactory.Create(builder => { });

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.ConstructServicesUsing(t =>
                {
                    if (t == typeof(ProfilePhotoUrlResolver))
                        return new ProfilePhotoUrlResolver(new BlobStorageOptions { PublicBaseUrl = "https://cdn.test" });
                    return Activator.CreateInstance(t)!;
                });
                cfg.AddProfile(new KurinModuleProfile());
            }, loggerFactory);
            _mapper = mapperConfig.CreateMapper();

            _handler = new GetMembersQueryHandler(_uowMock.Object, _mapper);
        }

        private static Member MakeMember(Guid groupKey, Guid kurinKey, string first, string last, string? blob = null) =>
            new()
            {
                MemberKey = Guid.NewGuid(),
                GroupKey = groupKey,
                KurinKey = kurinKey,
                FirstName = first,
                MiddleName = "M",
                LastName = last,
                Email = $"{first.ToLower()}@example.com",
                PhoneNumber = "123456",
                DateOfBirth = new DateOnly(2000, 1, 1),
                ProfilePhotoBlobName = blob
            };

        [Fact]
        public async Task Handle_GroupKeyOnly_ShouldReturnMembersFromGroup()
        {
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid(); // not used (handler treats KurinKey == empty for group path)
            var members = new List<Member>
            {
                MakeMember(groupKey, kurinKey, "A","One","a.png"),
                MakeMember(groupKey, kurinKey, "B","Two", null)
            };

            _memberRepoMock
                .Setup(r => r.GetAllAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            var query = new GetMembersQuery(groupKey, Guid.Empty);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().HaveCount(2);
            var list = result.Data!.ToList();
            list[0].FirstName.Should().Be("A");
            list[0].ProfilePhotoUrl.Should().Be("https://cdn.test/a.png");
            list[1].FirstName.Should().Be("B");
            list[1].ProfilePhotoUrl.Should().BeNull();

            _memberRepoMock.Verify(r => r.GetAllAsync(groupKey, It.IsAny<CancellationToken>()), Times.Once);
            _memberRepoMock.Verify(r => r.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_KurinKeyOnly_ShouldReturnMembersFromKurin()
        {
            var kurinKey = Guid.NewGuid();
            var m1 = MakeMember(Guid.NewGuid(), kurinKey, "C", "Three");
            var m2 = MakeMember(Guid.NewGuid(), kurinKey, "D", "Four");
            var members = new List<Member> { m1, m2 };

            _memberRepoMock
                .Setup(r => r.GetAllByKurinKeyAsync(kurinKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            var query = new GetMembersQuery(Guid.Empty, kurinKey);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().HaveCount(2);
            result.Data!.Select(x => x.FirstName).Should().BeEquivalentTo(new[] { "C", "D" });

            _memberRepoMock.Verify(r => r.GetAllByKurinKeyAsync(kurinKey, It.IsAny<CancellationToken>()), Times.Once);
            _memberRepoMock.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_BothKeysProvided_ShouldReturnBadRequest()
        {
            var query = new GetMembersQuery(Guid.NewGuid(), Guid.NewGuid());

            var result = await _handler.Handle(query, CancellationToken.None);

            result.Type.Should().Be(ResultType.BadRequest);
            result.Data.Should().BeNull();

            _memberRepoMock.Verify(r => r.GetAllAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
            _memberRepoMock.Verify(r => r.GetAllByKurinKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_GroupKeyOnly_NoMembers_ShouldReturnEmptySuccess()
        {
            var groupKey = Guid.NewGuid();
            _memberRepoMock
                .Setup(r => r.GetAllAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Member>());

            var result = await _handler.Handle(new GetMembersQuery(groupKey, Guid.Empty), CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrows_ShouldPropagateException()
        {
            var groupKey = Guid.NewGuid();
            var expected = new Exception("DB error");
            _memberRepoMock
                .Setup(r => r.GetAllAsync(groupKey, It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected);

            var query = new GetMembersQuery(groupKey, Guid.Empty);

            var ex = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
            ex.Should().BeSameAs(expected);
        }

        [Fact]
        public async Task Handle_MappingConsistency_ShouldMatchDirectMapping()
        {
            var groupKey = Guid.NewGuid();
            var kurinKey = Guid.NewGuid();
            var members = new List<Member>
            {
                MakeMember(groupKey, kurinKey, "X","One","pic1.jpg"),
                MakeMember(groupKey, kurinKey, "Y","Two","pic 2.png")
            };

            _memberRepoMock
                .Setup(r => r.GetAllAsync(groupKey, It.IsAny<CancellationToken>()))
                .ReturnsAsync(members);

            var result = await _handler.Handle(new GetMembersQuery(groupKey, Guid.Empty), CancellationToken.None);

            result.Type.Should().Be(ResultType.Success);
            var direct = _mapper.Map<IEnumerable<MemberResponse>>(members);
            result.Data.Should().BeEquivalentTo(direct);
        }
    }
}
