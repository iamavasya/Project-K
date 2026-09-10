using AutoMapper;
using FluentAssertions;
using Moq;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Dossier;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Dtos.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.MemberHandlers;

/// <summary>
/// The dossier is a box of folders: the index always says what is there, and a folder is opened only
/// when it is asked for. What the other modules hold is asked of them, so these tests check that the
/// contracts are called rather than that any particular table was read.
/// </summary>
public class GetMemberDossierHandlerTests
{
    private static readonly Guid MemberKey = Guid.NewGuid();

    private readonly Mock<IMemberUnitOfWork> _uow = new();
    private readonly Mock<IMemberRepository> _members = new();
    private readonly Mock<IMembershipDirectory> _memberships = new();
    private readonly Mock<IMemberProgressDirectory> _progress = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetMemberDossierHandler _handler;

    public GetMemberDossierHandlerTests()
    {
        _uow.Setup(u => u.Members).Returns(_members.Object);
        _members
            .Setup(r => r.GetByKeyAsync(MemberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Member
            {
                MemberKey = MemberKey,
                PublicId = "PL-ABCDE-12345",
                FirstName = "Оксана",
                LastName = "Тестова",
                Email = "oksana@example.com",
                PhoneNumber = "0500000000"
            });

        _mapper
            .Setup(m => m.Map<MemberResponse>(It.IsAny<Member>()))
            .Returns(new MemberResponse
            {
                MemberKey = MemberKey,
                FirstName = "Оксана",
                LastName = "Тестова",
                Email = "oksana@example.com",
                PhoneNumber = "0500000000",
                MiddleName = string.Empty,
                LeadershipHistories = [new LeadershipHistoryDto()],
                PlastLevelHistories = [new PlastLevelHistoryDto(), new PlastLevelHistoryDto()],
                Awards = [],
                Warnings = [new MemberWarningDto()]
            });

        _memberships
            .Setup(d => d.CountForMemberAsync(MemberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _memberships
            .Setup(d => d.GetForMemberAsync(MemberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Belonging(), Belonging()]);
        _progress
            .Setup(d => d.CountForMemberAsync(MemberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        _progress
            .Setup(d => d.GetForMemberAsync(MemberKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemberProgress(
                [new ProbeProgressRecord("upu-1", ProbeProgressStatus.Completed, Guid.NewGuid(), null, null)],
                []));

        _handler = new GetMemberDossierHandler(
            _uow.Object, _memberships.Object, _progress.Object, _mapper.Object);
    }

    private static MembershipRecord Belonging() => new(
        Guid.NewGuid(), Guid.NewGuid(), 42, KurinBranch.UPYu, null,
        null, null, MembershipKind.Youth, DateTime.UtcNow.AddYears(-2), null);

    private Task<ServiceResult<MemberDossierResponse>> Open(params string[] include)
        => _handler.Handle(new GetMemberDossier(MemberKey, include), CancellationToken.None);

    [Fact]
    public async Task WithNothingAsked_ShouldReturnTheIndexAndNoContents()
    {
        var result = await Open();

        result.Type.Should().Be(ResultType.Success);
        var dossier = result.Data!;

        dossier.Profile.PublicId.Should().Be("PL-ABCDE-12345");
        dossier.Folders.Should().HaveCount(6);
        dossier.Memberships.Should().BeNull();
        dossier.Offices.Should().BeNull();
        dossier.Levels.Should().BeNull();
        dossier.Awards.Should().BeNull();
        dossier.Warnings.Should().BeNull();
        dossier.Progress.Should().BeNull();

        _memberships.Verify(d => d.GetForMemberAsync(MemberKey, It.IsAny<CancellationToken>()), Times.Never);
        _progress.Verify(d => d.GetForMemberAsync(MemberKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TheIndex_ShouldCountEveryFolder_IncludingTheOnesOtherModulesHold()
    {
        var dossier = (await Open()).Data!;

        var counts = dossier.Folders.ToDictionary(folder => folder.Name, folder => folder.Count);
        counts[MemberDossierFolders.Memberships].Should().Be(2);
        counts[MemberDossierFolders.Offices].Should().Be(1);
        counts[MemberDossierFolders.Levels].Should().Be(2);
        counts[MemberDossierFolders.Awards].Should().Be(0);
        counts[MemberDossierFolders.Warnings].Should().Be(1);
        counts[MemberDossierFolders.Progress].Should().Be(3);
    }

    [Fact]
    public async Task AFolderThatWasRead_ShouldNotBeCountedTwice()
    {
        var dossier = (await Open(MemberDossierFolders.Memberships)).Data!;

        dossier.Memberships.Should().HaveCount(2);
        dossier.Folders.Single(f => f.Name == MemberDossierFolders.Memberships).Count.Should().Be(2);
        _memberships.Verify(
            d => d.CountForMemberAsync(MemberKey, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AskingForEverything_ShouldFillEveryFolder()
    {
        var dossier = (await Open([.. MemberDossierFolders.All])).Data!;

        dossier.Memberships.Should().NotBeNull();
        dossier.Offices.Should().NotBeNull();
        dossier.Levels.Should().NotBeNull();
        dossier.Awards.Should().NotBeNull();
        dossier.Warnings.Should().NotBeNull();
        dossier.Progress!.Probes.Should().ContainSingle();
    }

    [Fact]
    public async Task AFolderNameNobodyKnows_ShouldBeRefused_WithoutReadingAnything()
    {
        var result = await Open("salary");

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("UnknownDossierFolder");
        _members.Verify(r => r.GetByKeyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoSuchPerson_ShouldBeNotFound()
    {
        var result = await _handler.Handle(
            new GetMemberDossier(Guid.NewGuid(), []), CancellationToken.None);

        result.Type.Should().Be(ResultType.NotFound);
    }

    [Fact]
    public async Task WithoutAKey_ShouldBeRefused()
    {
        var result = await _handler.Handle(
            new GetMemberDossier(Guid.Empty, []), CancellationToken.None);

        result.Type.Should().Be(ResultType.BadRequest);
        result.ErrorCode.Should().Be("MemberKeyRequired");
    }
}
