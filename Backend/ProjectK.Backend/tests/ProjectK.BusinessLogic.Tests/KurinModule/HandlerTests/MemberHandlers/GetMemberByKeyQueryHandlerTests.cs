using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.BusinessLogic.MappingProfiles;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Get;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers;

public class GetMemberByKeyHandlerTests
{
    private readonly Mock<IMemberUnitOfWork> _uowMock;
    private readonly Mock<IMemberRepository> _memberRepoMock;
    private readonly Mock<IUnitOfWork> _kurinDataMock = new();
    private readonly Mock<IMembershipRepository> _membershipsMock = new();
    private readonly Mock<IKurinRepository> _kurinsMock = new();
    private readonly Mock<IMentorAssignmentRepository> _mentorRepoMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICurrentUserContext> _currentUserContextMock;
    private readonly GetMemberByKeyQueryHandler _handler;

    public GetMemberByKeyHandlerTests()
    {
        _memberRepoMock = new Mock<IMemberRepository>();
        _mentorRepoMock = new Mock<IMentorAssignmentRepository>();
        _uowMock = new Mock<IMemberUnitOfWork>();
        _uowMock.Setup(u => u.Members).Returns(_memberRepoMock.Object);

        _mapperMock = new Mock<IMapper>(MockBehavior.Strict);

        _currentUserContextMock = new Mock<ICurrentUserContext>();
        _currentUserContextMock.Setup(c => c.IsInRole(It.IsAny<string>())).Returns(true);

        _kurinDataMock.SetupGet(x => x.Memberships).Returns(_membershipsMock.Object);
        _kurinDataMock.SetupGet(x => x.Kurins).Returns(_kurinsMock.Object);
        _kurinDataMock.SetupGet(x => x.MentorAssignments).Returns(_mentorRepoMock.Object);
        _membershipsMock
            .Setup(x => x.GetActiveForMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _handler = new GetMemberByKeyQueryHandler(_uowMock.Object, _mapperMock.Object, _currentUserContextMock.Object, new Mock<IResourceScopeReader>().Object, _kurinDataMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberExists_ShouldReturnSuccessWithMappedData()
    {
        var memberKey = Guid.NewGuid();
        var groupKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var member = new Member
        {
            MemberKey = memberKey,
            FirstName = "Ivan",
            MiddleName = "I.",
            LastName = "Petrenko",
            Email = "ivan@example.com",
            PhoneNumber = "123456",
            DateOfBirth = new DateOnly(2001, 2, 3)
        };

        _membershipsMock
            .Setup(m => m.GetActiveForMemberAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Membership
            {
                MemberKey = memberKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                JoinedAtUtc = DateTime.UtcNow
            }]);

        _memberRepoMock
            .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        // Mock mapper behavior explicitly to avoid invoking real mapping (and its resolver dependencies)
        _mapperMock
            .Setup(m => m.Map<MemberResponse>(member))
            .Returns(new MemberResponse
            {
                MemberKey = member.MemberKey,
                FirstName = member.FirstName,
                MiddleName = member.MiddleName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = member.PhoneNumber,
                DateOfBirth = member.DateOfBirth,
                ProfilePhotoUrl = null
            });

        var query = new GetMemberByKeyQuery(memberKey);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Type.Should().Be(ResultType.Success);
        result.Data.Should().NotBeNull();
        result.Data!.MemberKey.Should().Be(memberKey);
        result.Data.GroupKey.Should().Be(groupKey);
        result.Data.KurinKey.Should().Be(kurinKey);
        result.Data.FirstName.Should().Be("Ivan");
        result.Data.LastName.Should().Be("Petrenko");

        _memberRepoMock.Verify(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.Verify(m => m.Map<MemberResponse>(member), Times.Once);
        _mapperMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_FillsTheGroupsTheyRunAsVykhovnykInTheKurinTheyAreSeenIn()
    {
        var memberKey = Guid.NewGuid();
        var userKey = Guid.NewGuid();
        var kurinKey = Guid.NewGuid();
        var groupOne = Guid.NewGuid();
        var member = new Member { MemberKey = memberKey, UserKey = userKey, FirstName = "Ivan", MiddleName = "", LastName = "P", Email = "i@e.com", PhoneNumber = "1" };

        _membershipsMock
            .Setup(m => m.GetActiveForMemberAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Membership { MemberKey = memberKey, KurinKey = kurinKey, JoinedAtUtc = DateTime.UtcNow }]);
        _memberRepoMock.Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _mapperMock.Setup(m => m.Map<MemberResponse>(member)).Returns(new MemberResponse { MemberKey = memberKey });
        _mentorRepoMock
            .Setup(r => r.GetActiveGroupsAsync(userKey, kurinKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new GroupRef(groupOne, "Gurtok 1"), new GroupRef(Guid.NewGuid(), "Gurtok 2")]);

        var result = await _handler.Handle(new GetMemberByKeyQuery(memberKey), CancellationToken.None);

        result.Data!.MentoredGroupNames.Should().Equal("Gurtok 1", "Gurtok 2");
        result.Data!.MentoredGroups.First().GroupKey.Should().Be(groupOne);
    }

    [Fact]
    public async Task Handle_WithoutAnAccount_RunsNoGroupAndDoesNotAsk()
    {
        var memberKey = Guid.NewGuid();
        var member = new Member { MemberKey = memberKey, FirstName = "Ivan", MiddleName = "", LastName = "P", Email = "i@e.com", PhoneNumber = "1" };

        _membershipsMock
            .Setup(m => m.GetActiveForMemberAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Membership { MemberKey = memberKey, KurinKey = Guid.NewGuid(), JoinedAtUtc = DateTime.UtcNow }]);
        _memberRepoMock.Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>())).ReturnsAsync(member);
        _mapperMock.Setup(m => m.Map<MemberResponse>(member)).Returns(new MemberResponse { MemberKey = memberKey });

        var result = await _handler.Handle(new GetMemberByKeyQuery(memberKey), CancellationToken.None);

        result.Data!.MentoredGroupNames.Should().BeEmpty();
        _mentorRepoMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenMemberDoesNotExist_ShouldReturnNotFound()
    {
        var memberKey = Guid.NewGuid();
        _memberRepoMock
            .Setup(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Member)null!);

        var query = new GetMemberByKeyQuery(memberKey);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.Type.Should().Be(ResultType.NotFound);
        result.Data.Should().BeNull();

        _memberRepoMock.Verify(r => r.GetByKeyAsync(memberKey, It.IsAny<CancellationToken>()), Times.Once);
        _mapperMock.VerifyNoOtherCalls();
    }
}
